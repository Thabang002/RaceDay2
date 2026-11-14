# RaceDay API xUnit integration tests

These tests target the uploaded `RaceDay` ASP.NET Core API project and use `WebApplicationFactory<Program>` plus EF Core's InMemory provider. They exercise the actual HTTP endpoints, JWT authentication, and role authorization without connecting to SQL Server LocalDB.

## Files
- `RaceDay.Tests.csproj`
- `RaceDayApiFactory.cs`
- `AuthAndAuthorizationTests.cs`

## Add to your project
Extract the `RaceDay.Tests` folder next to the existing `RaceDay` folder, so the structure is:

```
parent-folder/
  RaceDay/
    RaceDay.csproj
  RaceDay.Tests/
    RaceDay.Tests.csproj
    RaceDayApiFactory.cs
    AuthAndAuthorizationTests.cs
```

## Run in Visual Studio
1. Open the existing solution.
2. Right-click the solution in Solution Explorer and choose **Add > Existing Project**.
3. Select `RaceDay.Tests/RaceDay.Tests.csproj`.
4. Build the solution.
5. Open **Test > Test Explorer**, then choose **Run All Tests**.

Or from a terminal opened in the parent folder:

```powershell
dotnet test .\RaceDay.Tests\RaceDay.Tests.csproj
```

The first run needs internet access to restore NuGet packages.

## Coverage
- Successful registration and token creation
- Duplicate registration and invalid role rejection
- Successful login and invalid password rejection
- Unauthenticated requests rejected by protected endpoints
- Participant cannot create/manage events
- Organiser can create, update, and delete their own events
- An organiser cannot update or delete another organiser's event
- Participant can enrol; enrolment fields and database record are checked
- Duplicate enrolment is rejected
- Organiser and unauthenticated users cannot enrol as participants
- A participant cannot read another participant's enrolment

These are integration tests (HTTP endpoint tests), which provide stronger evidence for authentication and authorization than direct controller unit tests. They have not been executed in this environment; run them in Visual Studio or with `dotnet test` and resolve any environment-specific build/package issues.
