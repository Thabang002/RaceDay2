# RaceDay – Part 3 (MVC front end, Azure Blob Storage, Docker)

RaceDay is a race-event platform for **Organisers** (create and run events) and **Participants** (find races, enrol, see results).

| Project | What it is |
|---|---|
| `RaceDay/` | Part 2 **Web API** (EF Core + SQL Server, JWT). Extended in Part 3 with event distance/type, banner images, results and profile pictures stored in **Azure Blob Storage**. |
| `RaceDay.Web/` | Part 3 **ASP.NET Core MVC** front end. It talks to the API only: no database access and no business rules. |
| `RaceDay.Tests/` | xUnit integration tests for the API (the Part 2 tests plus new Part 3 tests). |
| `docker-compose.yml` | Starts the MVC container (and an optional Azurite blob emulator) with one command. |

```
Browser ──► RaceDay.Web (MVC, session) ──HTTP + JWT──► RaceDay API ──► SQL Server
                 │ /media/{blob}                         │ /api/files/{blob}
                 └───────────── image proxy ────────────►└──► Azure Blob Storage
```

The MVC app never contacts Azure. Images are uploaded **to the API** (`multipart/form-data`). The API stores them in a **private** blob container and streams them back through `GET /api/files/{blobName}`. The MVC `/media/...` route proxies that endpoint, so the browser never sees an Azure URL or key either.

---

## 1. Prerequisites

* Visual Studio 2022 (17.14+) or Visual Studio 2026 with the **ASP.NET and web development** workload and the **.NET 10 SDK**
* SQL Server LocalDB (installed with Visual Studio)
* **Docker Desktop** (for the container)
* An **Azure Storage account**, or **Azurite** for local development (see step 2)

## 2. First-time setup in Visual Studio

1. **Open `RaceDay.slnx`** in the root folder. It contains all three projects.

2. **Configure Azure Blob Storage for the API.** Right-click the **RaceDay** project, choose **Manage User Secrets**, and paste your connection string. You'll find it in the Azure Portal under *Storage account → Security + networking → Access keys*:
   ```json
   {
     "AzureStorage": {
       "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=<name>;AccountKey=<key>;EndpointSuffix=core.windows.net",
       "ContainerName": "raceday-images"
     }
   }
   ```
   To work offline, leave `appsettings.json` as it is (`UseDevelopmentStorage=true`) and start Azurite. Either run `docker compose --profile storage up -d azurite`, or run `npx azurite-blob` (needs Node.js). The container is created automatically on the first upload.

3. **Update the database.** Part 3 adds new columns: event distance, type, banner, results-published flag and profile fields. Open **Tools → NuGet Package Manager → Package Manager Console**, set **Default project** to `RaceDay`, then run:
   ```powershell
   Add-Migration Part3_EventDetails_Results_Images
   Update-Database
   ```
   Alternatively, from a terminal in the root folder: `dotnet ef migrations add Part3_EventDetails_Results_Images --project RaceDay` and then `dotnet ef database update --project RaceDay`.

4. **Run both projects together.** Right-click the solution, choose **Configure Startup Projects…**, then **Multiple startup projects**, and set **RaceDay** and **RaceDay.Web** to *Start*.
   * The API starts on `http://localhost:5163` (Swagger at `/swagger`).
   * The MVC app starts on `https://localhost:7001`.
   * The MVC app reads the API address from `RaceDay.Web/appsettings.json → Api:BaseUrl`.

5. **Run the tests** from **Test → Test Explorer → Run All**, or run `dotnet test` in the root folder.

## 3. Docker

The MVC app is containerised with a multi-stage `RaceDay.Web/Dockerfile`:

* The SDK image restores and publishes.
* The slim `aspnet` runtime image runs the app as the non-root `app` user on port 8080.
* `.dockerignore` keeps `bin/obj` and other unneeded files out of the build context.

Keep the **API running on your machine** (step 4 above). The container reaches it through `host.docker.internal`.

**Build the image** (from the root folder):
```bash
docker build -t raceday-web ./RaceDay.Web
```

**Start the app with a single command:**
```bash
docker run -d --name raceday-web -p 8080:8080 --add-host=host.docker.internal:host-gateway -e Api__BaseUrl=http://host.docker.internal:5163/ raceday-web
```

Then open **http://localhost:8080**.

You can also build and run in one step with Docker Compose:
```bash
docker compose up --build
```

In Visual Studio, you can pick the **Container (Dockerfile)** launch profile for RaceDay.Web.

The container behaves identically to the local app:

* Configuration comes from environment variables (`Api__BaseUrl`).
* Culture is fixed to `en-GB`, so dates and decimals parse the same way on Windows and Linux.
* Sessions and anti-forgery work over plain HTTP inside the container.

To stop and remove it: `docker rm -f raceday-web`.

## 4. Using the application

### Authentication & roles
* Register at **/Account/Register** and choose *Participant* or *Organiser*. Log in at **/Account/Login**.
* After login, the API's JWT, user id, name, e-mail and role are kept in **ASP.NET Core session**. Every API call sends the token as a Bearer header.
* `[SessionAuthorize(Roles = "...")]` protects every page:
  * Anonymous users are redirected to Login, with a return URL.
  * Users with the wrong role are sent to *Access denied*.
  * If the JWT expires or is rejected, the session is cleared and the user is asked to log in again.
* **Log out** (in the menu) clears the session and returns the user to the login page.

### Organiser – "Race Control" (sidebar navigation, `/Organiser/...`)
| Page | URL |
|---|---|
| Dashboard: totals, enrolments per event, upcoming dates, results still to capture | `/Organiser/Dashboard` |
| My events: create / edit / delete (name, description, date, start time, venue, distance, event type, capacity, status, **banner upload**) | `/Organiser/Events` |
| Event overview with banner | `/Organiser/Events/Details/{id}` |
| Categories (age or distance), with quick-fill presets | `/Organiser/Categories?eventId={id}` |
| Enrolments: participant, category, status, confirm / mark pending, filters | `/Organiser/Enrolments?eventId={id}` |
| Results entry: finish time, position (manual or **auto-rank**), status (Finished/DNF/DNS/DSQ), **publish** | `/Organiser/Results?eventId={id}` |
| Add venue | `/Organiser/Locations/Create` |

### Participant (top navigation, `/Participant/...`)
| Page | URL |
|---|---|
| Home: upcoming events with search, type, distance, venue and date filters | `/Participant/Events` |
| Event details, banner and categories, then **Enrol now** (recorded in the API immediately) | `/Participant/Events/Details/{id}` |
| My enrolments: upcoming and past, colour-coded status, withdraw | `/Participant/Enrolments` |
| My results: event, date, category, finish time, position such as "47th out of 312 finishers", pace | `/Participant/Results` |
| Profile: personal details, password, **profile picture upload** (shared with organisers) | `/Profile` |

### Enrolment status colours
| Status | Meaning | Colour |
|---|---|---|
| **Confirmed** | Organiser confirmed or marked the entry as paid | 🟢 green |
| **Pending** | Enrolled, waiting for the organiser to confirm | 🟠 amber |
| **Withdrawn / Cancelled** | No longer active | 🔴 red |

## 5. API changes in Part 3

| Method & route | Who | Purpose |
|---|---|---|
| `GET /api/events?search=&eventType=&locationId=&from=&to=&upcoming=&minDistance=&maxDistance=` | public | Filtering for the participant home page |
| `GET /api/events/types` | public | Supported event types |
| `GET /api/events/mine` | Organiser | Dashboard summary with enrolment counts |
| `POST /api/events/{id}/banner` | Organiser (owner) | Upload a banner to Azure Blob Storage |
| `GET /api/files/{**blobName}` | public | Stream an image from the private blob container |
| `POST /api/users/profile/picture` | logged in | Upload a profile picture to Azure Blob Storage |
| `GET /api/events/{id}/results` | Organiser (owner) | Results sheet (every active enrolment) |
| `PUT /api/events/{id}/results` | Organiser (owner) | Save times and positions (`autoRank` optional); only after the event date |
| `POST /api/events/{id}/results/publish?publish=true` | Organiser (owner) | Publish or unpublish results |
| `GET /api/users/me/results` | Participant | Personal race history, with the total number of finishers |

Other changes:

* `EventDto`, `CategoryDto`, `EnrolmentDto` and `UserDto` carry the new fields. `EnrolmentDto.DisplayStatus` is `Confirmed`, `Pending` or `Withdrawn`.
* Enrolment rules were tightened: no enrolling in past, closed or full events.
* Uploads are validated (JPG/PNG/WEBP/GIF, 5 MB maximum).

## 6. Error handling

* The API returns `{ "message": "..." }` or ProblemDetails. The MVC client turns these into friendly text, shown next to the relevant form field or as an alert.
* If the API is unreachable, the user sees a "couldn't reach the RaceDay service" message instead of a crash.
* Unhandled exceptions show a branded error page (`/Home/Error`). 404, 403 and other status codes show `/Home/StatusCode`. Raw exception details are never shown.
* Forms validate on the client (jQuery unobtrusive validation) and again on the server (DataAnnotations). The API then validates everything a third time.
