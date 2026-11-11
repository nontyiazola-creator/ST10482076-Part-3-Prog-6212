using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;
using System.Text.Json;

namespace RaceDay.Mvc.Controllers
{
    public class AuthController : Controller
    {
        private readonly RaceDayApiClient _api;

        public AuthController(RaceDayApiClient api)
        {
            _api = api;
        }

        // ---------- REGISTER ----------

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var request = new
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Email = vm.Email,
                Password = vm.Password,
                Role = vm.Role
            };

            var response = await _api.SendAsync(HttpMethod.Post, "api/auth/register", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("", error);
                return View(vm);
            }

            TempData["Success"] = "Registration successful. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        // ---------- LOGIN ----------

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var response = await _api.SendAsync(HttpMethod.Post, "api/auth/login",
                new { Email = vm.Email, Password = vm.Password });

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(vm);
            }

            // 1. Keep the API session cookie
            string? apiCookie = RaceDayApiClient.ExtractSessionCookie(response);
            if (!string.IsNullOrWhiteSpace(apiCookie))
                HttpContext.Session.SetString("ApiSessionCookie", apiCookie);

            // 2. Read role and userID from the login response
            var loginJson = await response.Content.ReadAsStringAsync();
            HttpContext.Session.SetString("Role", ReadField(loginJson, "role") ?? "Participant");
            HttpContext.Session.SetString("UserID", ReadField(loginJson, "userID") ?? "");
            HttpContext.Session.SetString("Email", vm.Email);

            // 3. Fetch the display name from the profile endpoint
            var profileResponse = await _api.SendAsync(HttpMethod.Get, "api/profile");
            if (profileResponse.IsSuccessStatusCode)
            {
                var profileJson = await profileResponse.Content.ReadAsStringAsync();
                var first = ReadField(profileJson, "firstName");
                var last = ReadField(profileJson, "lastName");
                HttpContext.Session.SetString("DisplayName", $"{first} {last}".Trim());
            }

            return RedirectToAction("Index", "Home");
        }

        // ---------- LOGOUT ----------

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _api.SendAsync(HttpMethod.Post, "api/auth/logout");
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ---------- HELPER ----------

        // Reads one field from a JSON object, ignoring upper/lower case
        private static string? ReadField(string json, string name)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                        return prop.Value.ValueKind == JsonValueKind.String
                            ? prop.Value.GetString()
                            : prop.Value.ToString();
                }
            }
            catch (JsonException) { }
            return null;
        }
    }
}