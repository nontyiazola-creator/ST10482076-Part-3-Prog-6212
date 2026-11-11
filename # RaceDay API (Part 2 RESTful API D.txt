# RaceDay API (Part 2: RESTful API Development)

A RESTful API for managing running, walking and cycling events, built with ASP.NET Core Web API (.NET 8), Entity Framework Core (Code-First) and SQL Server. It is the backend for the RaceDay system and follows the endpoint plan and ERD from Part 1.

**Demo video:** [PASTE UNLISTED YOUTUBE LINK HERE]

## Technologies

- ASP.NET Core Web API (.NET 8, C#)
- Entity Framework Core 8 (Code-First) with SQL Server
- Session-based authentication (server-side sessions)
- BCrypt for password hashing
- Swagger (Swashbuckle) for live API documentation
- xUnit with the EF Core in-memory provider for unit tests
- GitHub Actions for CI

## The two roles

**Organiser**
- Creates, updates and deletes their own events
- Defines categories (for example Under 20, Senior, 10km, 21km) for their events
- Views all enrolments for their own events
- Captures finish times and finishing positions
- Views and updates their own profile

**Participant**
- Views all events and their categories
- Enrols in an event by choosing a category
- Views their own enrolments and their own results
- Views and updates their own profile

The role is chosen at registration. Organiser-only endpoints return **403 Forbidden** to Participants, and Participant-only endpoints return **403 Forbidden** to Organisers. Unauthenticated requests return **401 Unauthorized**.

## Authentication

- Passwords are hashed with BCrypt and never stored in plain text.
- On login, the user's ID and role are stored in a server-side session and a session cookie is issued.
- A custom `[SessionAuthorize]` filter protects every endpoint except register and login. `[SessionAuthorize("Organiser")]` restricts an endpoint to a single role.

## Setup and run instructions

### Prerequisites
- Visual Studio 2022 with the ASP.NET and web development workload
- .NET 8 SDK
- SQL Server (LocalDB, Express or full)

### Steps
1. Clone the repository and switch to the Part 2 branch:
```
   git clone https://github.com/[YOUR-USERNAME]/[YOUR-REP
