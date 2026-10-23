# RaceDay API

## Overview

RaceDay is a web API developed using C# and ASP.NET Core. It provides functionality for user authentication, role-based access control, event management, and participant enrolments.

The API allows users to register and log in, while organisers can manage their own events and participants can enrol in available events.

## Features

- User registration and login.
- Authentication using access tokens.
- Role-based authorisation for Organisers and Participants.
- Event creation, updating, and deletion by authorised organisers.
- Participant enrolment in events.
- Automated unit and integration testing using xUnit.

## Technologies Used

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- xUnit
- Git and GitHub

## User Roles

### Organiser

Organisers can access authorised event management functionality, including creating, updating, and deleting their own events.

### Participant

Participants can access participant functionality, including enrolling in events, subject to the API's authorisation rules.

## Getting Started

### Prerequisites

Install the following software:

- .NET SDK compatible with the project.
- Visual Studio or Visual Studio Code.
- SQL Server or another database provider configured by the application.
- Git.

  ### CI workflows
  

### Installation

1. Clone the repository:

   ```bash
   git clone <your-github-repository-url>
   ```

2. Navigate to the project directory:

   ```bash
   cd RaceDay
   ```

3. Restore the project dependencies:

   ```bash
   dotnet restore
   ```

4. Configure the database connection string in the appropriate application settings file.

5. Apply database migrations if the project uses Entity Framework Core migrations:

   ```bash
   dotnet ef database update
   ```

6. Run the API:

   ```bash
   dotnet run
   ```

Use the URL displayed in the terminal to access the running API. If Swagger is configured, open the Swagger endpoint to view and test the available API routes.

## Running the Tests

The project includes a separate `RaceDay.Tests` project for automated testing.

From the solution directory, run:

```bash
dotnet test
```

To run only the test project:

```bash
dotnet test RaceDay.Tests/RaceDay.Tests.csproj
```

The tests are intended to verify authentication, role-based access control, event management permissions, and participant enrolment rules.

## Configuration

Before running the application, ensure that:

- The database connection string is correctly configured.
- Required database migrations have been applied.
- Authentication settings are configured.
- Any required environment variables or secrets are supplied.

Do not commit passwords, private keys, database credentials, or other sensitive information to GitHub.

## Project Structure

The solution is organised into an API project and a separate testing project.

```text
RaceDay/
    Controllers/
    Models/
    Data/
    Program.cs
    appsettings.json

RaceDay.Tests/
    AuthAndAuthorizationTests.cs
    RaceDayApiFactory.cs

README.md
```

The folders and filenames shown above are examples. The actual structure may differ depending on the implementation.

## Version Control

Git is used to track changes to the source code and tests. GitHub hosts the repository for version control and collaboration.

To check for uncommitted changes:

```bash
git status
```

To stage, commit, and push changes:

```bash
git add .
git commit -m "Update RaceDay API"
git push
```

## Future Improvements

- Add more automated tests for API endpoints.
- Improve validation and error handling.
- Add API documentation where necessary.
- Strengthen authentication and security.
- Add continuous integration to run tests automatically when changes are pushed.

## License

No licence has been specified for this project. Add an appropriate licence before distributing or reusing the code.
