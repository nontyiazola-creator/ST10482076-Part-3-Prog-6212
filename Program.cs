using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using System.Reflection;

namespace RaceDay.API
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Database
            builder.Services.AddDbContext<RaceDayContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

            // 2. Session management
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = ".RaceDay.Session";
            });

            // 3. Controllers + Swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Middleware pipeline (order matters!)
            app.UseHttpsRedirection();
            app.UseSession();
            app.MapControllers();

            app.Run();
        }
    }
}