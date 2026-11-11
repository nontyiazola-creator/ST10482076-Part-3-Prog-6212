# RaceDay.API
ST10482076-Prog-6212-Part-2
RaceDay API (Part 2: RESTful API Development)
A RESTful API for managing running, walking and cycling events, built with ASP.NET Core Web API (.NET 8), Entity Framework Core (Code-First) and SQL Server. It is the backend for the RaceDay system and follows the endpoint plan and ERD from Part 1.

Demo video: [PASTE UNLISTED YOUTUBE LINK HERE]

Technologies
ASP.NET Core Web API (.NET 8, C#)
Entity Framework Core 8 (Code-First) with SQL Server
Session-based authentication (server-side sessions)
BCrypt for password hashing
Swagger (Swashbuckle) for live API documentation
xUnit with the EF Core in-memory provider for unit tests
GitHub Actions for CI
The two roles
Organiser

Creates, updates and deletes their own events
Defines categories (for example Under 20, Senior, 10km, 21km) for their events
Views all enrolments for their own events
Captures finish times and finishing positions
Views and updates their own profile
Participant

Views all events and their categories
Enrols in an event by choosing a category
Views their own enrolments and their own results
Views and updates their own profile
The role is chosen at registration. Organiser-only endpoints return 403 Forbidden to Participants, and Participant-only endpoints return 403 Forbidden to Organisers. Unauthenticated requests return 401 Unauthorized.

Authentication
Passwords are hashed with BCrypt and never stored in plain text.
On login, the user's ID and role are stored in a server-side session and a session cookie is issued.
A custom [SessionAuthorize] filter protects every endpoint except register and login. [SessionAuthorize("Organiser")] restricts an endpoint to a single role.
Setup and run instructions
Prerequisites
Visual Studio 2022 with the ASP.NET and web development workload
.NET 8 SDK
SQL Server (LocalDB, Express or full)
Steps
Clone the repository and switch to the Part 2 branch:
   git clone https://github.com/[YOUR-USERNAME]/[YOUR-REPO].git
   cd [YOUR-REPO]
   git checkout part2-api
Open RaceDay.API.sln in Visual Studio.
Open RaceDay.API/appsettings.json and set the ConnectionStrings:Default value to point to your SQL Server instance.
Open Tools > NuGet Package Manager > Package Manager Console, set Default project to RaceDay.API, and run:
   Update-Database
This creates the tables and seeds the two roles (Organiser = 1, Participant = 2). 5. Set RaceDay.API as the startup project and press F5. 6. Swagger UI opens in the browser. If it doesn't, add /swagger to the address.

Trying it in Swagger
POST /api/auth/register to create an Organiser, then POST /api/auth/login.
POST /api/events, then POST /api/events/{eventId}/categories.
POST /api/auth/logout, then register and log in as a Participant.
POST /api/enrolments to enrol in the event.
Log in as the Organiser again and call PUT /api/enrolments/{enrolmentId}/result with a finish time such as "00:45:30".
Log in as the Participant and call GET /api/results/mine.
Swagger runs in the same browser, so it sends the session cookie automatically.

Endpoints
Area	Method and route	Access
Auth	POST /api/auth/register	Public
Auth	POST /api/auth/login	Public
Auth	POST /api/auth/logout	Logged in
Profile	GET /api/profile	Both roles
Profile	PUT /api/profile	Both roles
Events	GET /api/events, GET /api/events/{id}	Both roles
Events	POST /api/events	Organiser
Events	PUT /api/events/{id}, DELETE /api/events/{id}	Organiser (own events)
Categories	GET /api/events/{eventId}/categories	Both roles
Categories	POST /api/events/{eventId}/categories	Organiser (own events)
Categories	PUT /api/categories/{id}, DELETE /api/categories/{id}	Organiser (own events)
Enrolments	POST /api/enrolments	Participant
Enrolments	GET /api/enrolments/mine	Participant
Enrolments	GET /api/events/{eventId}/enrolments	Organiser (own events)
Results	PUT /api/enrolments/{enrolmentId}/result	Organiser (own events)
Results	GET /api/events/{eventId}/results	Organiser (own events)
Results	GET /api/results/mine	Participant
Project structure
RaceDay.API/
  Controllers/   Auth, Events, Categories, Enrolments, Results, Profile
  Data/          RaceDayContext (EF Core DbContext, seed data, constraints)
  Dtos/          Request models
  Filters/       SessionAuthorizeAttribute (login and role checks)
  Migrations/    EF Core Code-First migrations
  Models/        User, Role, Event, Category, Enrolment, Result
  Program.cs     Services, session, Swagger and middleware pipeline
RaceDay.API.Test/
  TestHelper.cs  Fake session and in-memory database helpers
  APITests.cs    Unit tests
Unit tests
Run them in Visual Studio (Test > Run All Tests) or from the command line:

dotnet test
The 22 tests use an in-memory database, so no SQL Server is needed. Each area covers both success and failure cases:

Registration and login: valid registration, password hashing, duplicate email, invalid role, valid login starts a session, wrong password is rejected
Event management: create, invalid event type, update own event, update another Organiser's event (403), delete, delete a missing event (404)
Role-based access: not logged in (401), Participant on an Organiser endpoint (403), Organiser on a Participant endpoint (403), correct role allowed, controllers carry the authorisation attribute
Event enrolments: enrolment records participant, event and category, duplicate enrolment rejected, category from another event rejected, missing event, Organiser can view only their own event's enrolments
Continuous integration
A GitHub Actions workflow (.github/workflows/dotnet.yml) restores, builds and runs all unit tests on every push.

Successful GitHub Actions build
