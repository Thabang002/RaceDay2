using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Data;
using RaceDay.Models.DTOs;
using RaceDay.Models.Entities;
using Xunit;

namespace RaceDay.Tests;

/// <summary>Tests for the endpoints added in Part 3 (event fields, results, my-results, uploads).</summary>
public sealed class Part3ApiTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;
    public Part3ApiTests(RaceDayApiFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private async Task<(string Token, int UserId)> RegisterAsync(HttpClient client, string role)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"{role.ToLower()}-{Guid.NewGuid():N}@example.com",
            password = "SecurePass123!",
            fullName = $"{role} Tester",
            role
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>(Json);
        return (body!.Token, body.User.UserId);
    }

    private static void Authenticate(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task EventTypes_ArePublic()
    {
        using var client = _factory.CreateClient();
        var types = await client.GetFromJsonAsync<List<string>>("/api/events/types", Json);
        Assert.NotNull(types);
        Assert.Contains("Marathon", types);
    }

    [Fact]
    public async Task Organiser_CreatesEventWithDistanceAndType_AndSeesItOnDashboard()
    {
        using var client = _factory.CreateClient();
        var (token, _) = await RegisterAsync(client, "Organiser");
        var seeded = await _factory.SeedEventAsync();
        Authenticate(client, token);

        var create = await client.PostAsJsonAsync("/api/events", new CreateEventDto(
            "Sunrise 21K", "Half marathon", DateTime.UtcNow.Date.AddDays(20), new TimeSpan(6, 30, 0),
            seeded.LocationId, 300, 21.1m, "Half Marathon"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = JsonDocument.Parse(await create.Content.ReadAsStringAsync()).RootElement.GetProperty("eventId").GetInt32();

        var ev = await client.GetFromJsonAsync<EventDto>($"/api/events/{id}", Json);
        Assert.Equal(21.1m, ev!.DistanceKm);
        Assert.Equal("Half Marathon", ev.EventType);

        var mine = await client.GetFromJsonAsync<List<OrganiserEventSummaryDto>>("/api/events/mine", Json);
        Assert.Contains(mine!, e => e.EventId == id && e.EnrolmentCount == 0);
    }

    [Fact]
    public async Task InvalidEventType_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var (token, _) = await RegisterAsync(client, "Organiser");
        var seeded = await _factory.SeedEventAsync();
        Authenticate(client, token);

        var create = await client.PostAsJsonAsync("/api/events", new CreateEventDto(
            "Bad type", null, DateTime.UtcNow.Date.AddDays(20), new TimeSpan(6, 30, 0),
            seeded.LocationId, 10, 5m, "Underwater Basket Weaving"));
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    [Fact]
    public async Task BannerUpload_RejectsNonImageFiles_BeforeTouchingStorage()
    {
        using var client = _factory.CreateClient();
        var (token, organiserId) = await RegisterAsync(client, "Organiser");
        var seeded = await _factory.SeedEventAsync(organiserId);
        Authenticate(client, token);

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent("not an image"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(file, "file", "notes.txt");

        var response = await client.PostAsync($"/api/events/{seeded.EventId}/banner", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Results_CanBeCapturedAutoRankedPublished_AndShowInMyResults()
    {
        using var client = _factory.CreateClient();
        var (orgToken, organiserId) = await RegisterAsync(client, "Organiser");
        var (p1Token, p1) = await RegisterAsync(client, "Participant");
        var (_, p2) = await RegisterAsync(client, "Participant");

        // Event that took place yesterday, with two enrolled participants.
        var seeded = await _factory.SeedEventAsync(organiserId, "Yesterday's Race");
        int e1, e2;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
            var ev = await db.Events.FindAsync(seeded.EventId);
            ev!.Date = DateTime.UtcNow.Date.AddDays(-1);
            var a = new Enrolment { UserId = p1, EventId = seeded.EventId, CategoryId = seeded.CategoryId, EntryNumber = "A1" };
            var b = new Enrolment { UserId = p2, EventId = seeded.EventId, CategoryId = seeded.CategoryId, EntryNumber = "B2" };
            db.Enrolments.AddRange(a, b);
            await db.SaveChangesAsync();
            e1 = a.EnrolmentId; e2 = b.EnrolmentId;
        }

        Authenticate(client, orgToken);

        // Publishing with no results is rejected.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsync($"/api/events/{seeded.EventId}/results/publish", null)).StatusCode);

        var save = await client.PutAsJsonAsync($"/api/events/{seeded.EventId}/results", new SaveResultsDto(
            new List<SaveResultDto>
            {
                new(e1, new TimeSpan(0, 52, 10), null, null, null),   // slower
                new(e2, new TimeSpan(0, 45, 0), null, null, null)     // faster
            }, AutoRank: true));
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        var sheet = await save.Content.ReadFromJsonAsync<List<ResultEntryDto>>(Json);
        Assert.Equal(1, sheet!.Single(r => r.EnrolmentId == e2).Position);
        Assert.Equal(2, sheet.Single(r => r.EnrolmentId == e1).Position);

        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsync($"/api/events/{seeded.EventId}/results/publish", null)).StatusCode);

        Authenticate(client, p1Token);
        var mine = await client.GetFromJsonAsync<List<MyResultDto>>("/api/users/me/results", Json);
        var row = Assert.Single(mine!);
        Assert.Equal(2, row.Position);
        Assert.Equal(2, row.TotalFinishers);
        Assert.Equal(new TimeSpan(0, 52, 10), row.FinishTime);
    }

    [Fact]
    public async Task Results_CannotBeCapturedBeforeTheEvent()
    {
        using var client = _factory.CreateClient();
        var (token, organiserId) = await RegisterAsync(client, "Organiser");
        var seeded = await _factory.SeedEventAsync(organiserId); // 30 days in the future
        Authenticate(client, token);

        var response = await client.PutAsJsonAsync($"/api/events/{seeded.EventId}/results",
            new SaveResultsDto(new List<SaveResultDto>(), false));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrolment_ShowsPendingUntilOrganiserConfirms()
    {
        using var client = _factory.CreateClient();
        var (orgToken, organiserId) = await RegisterAsync(client, "Organiser");
        var (pToken, pId) = await RegisterAsync(client, "Participant");
        var seeded = await _factory.SeedEventAsync(organiserId);

        Authenticate(client, pToken);
        var enrol = await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrol", new CreateEnrolmentDto(seeded.CategoryId, null));
        var enrolment = await enrol.Content.ReadFromJsonAsync<EnrolmentDto>(Json);
        Assert.Equal("Pending", enrolment!.DisplayStatus);

        Authenticate(client, orgToken);
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/payment", new UpdatePaymentDto("Paid"))).StatusCode);

        Authenticate(client, pToken);
        var list = await client.GetFromJsonAsync<List<EnrolmentDto>>($"/api/users/{pId}/enrolments", Json);
        Assert.Equal("Confirmed", list!.Single().DisplayStatus);
    }
}
