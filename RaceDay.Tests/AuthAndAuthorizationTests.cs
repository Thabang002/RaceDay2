using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RaceDay.Data;
using RaceDay.Models.DTOs;
using RaceDay.Models.Entities;
using Xunit;

namespace RaceDay.Tests;

public sealed class AuthAndAuthorizationTests : IClassFixture<RaceDayApiFactory>
{
    private readonly RaceDayApiFactory _factory;
    public AuthAndAuthorizationTests(RaceDayApiFactory factory) => _factory = factory;

    private HttpClient CreateClient() => _factory.CreateClient();

    private static object Registration(string email, string role = "Participant") => new
    {
        email,
        password = "SecurePass123!",
        fullName = "Test User",
        role
    };

    private async Task<string> RegisterAndGetTokenAsync(HttpClient client, string email, string role)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", Registration(email, role));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        return body.Token;
    }

    private static void Authenticate(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    [Fact]
    public async Task Register_ValidParticipant_ReturnsCreatedAndToken()
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            Registration($"participant-{Guid.NewGuid():N}@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("Participant", result.User.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        using var client = CreateClient();
        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/auth/register", Registration(email))).StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/register", Registration(email));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            Registration($"invalid-{Guid.NewGuid():N}@example.com", "Admin"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken_AndInvalidPasswordReturnsUnauthorized()
    {
        using var client = CreateClient();
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/auth/register", Registration(email));

        var success = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDto(email, "SecurePass123!"));
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        var body = await success.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));

        var failure = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDto(email, "WrongPassword!"));
        Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
    }

    [Fact]
    public async Task ProtectedProfile_UnauthenticatedReturnsUnauthorized_AuthenticatedReturnsOk()
    {
        using var client = CreateClient();
        var email = $"profile-{Guid.NewGuid():N}@example.com";
        var token = await RegisterAndGetTokenAsync(client, email, "Participant");

        var unauthenticated = await client.GetAsync("/api/users/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        Authenticate(client, token);
        var authenticated = await client.GetAsync("/api/users/profile");
        Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_OrganiserCanCreateUpdateAndDeleteOwnEvent()
    {
        using var client = CreateClient();
        var participantToken = await RegisterAndGetTokenAsync(client,
            $"participant-{Guid.NewGuid():N}@example.com", "Participant");
        var organiserEmail = $"organiser-{Guid.NewGuid():N}@example.com";
        var organiserToken = await RegisterAndGetTokenAsync(client, organiserEmail, "Organiser");

        var seeded = await _factory.SeedEventAsync();
        var dto = new CreateEventDto("New Race", "Created by test",
            DateTime.UtcNow.Date.AddDays(60), new TimeSpan(10, 0, 0), seeded.LocationId, 50);

        Authenticate(client, participantToken);
        var denied = await client.PostAsJsonAsync("/api/events", dto);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        Authenticate(client, organiserToken);
        var created = await client.PostAsJsonAsync("/api/events", dto);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = System.Text.Json.JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var eventId = createdJson.RootElement.GetProperty("eventId").GetInt32();

        var update = new UpdateEventDto("Updated Race", "Updated description",
            DateTime.UtcNow.Date.AddDays(70), new TimeSpan(11, 0, 0), seeded.LocationId, 60, "Scheduled");
        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync($"/api/events/{eventId}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/api/events/{eventId}")).StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedCannotCreateEvent_AndParticipantCannotManageEvents()
    {
        using var client = CreateClient();
        var seeded = await _factory.SeedEventAsync();
        var dto = new CreateEventDto("Denied Race", null, DateTime.UtcNow.Date.AddDays(20),
            new TimeSpan(9, 0, 0), seeded.LocationId, 20);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/events", dto)).StatusCode);

        var participantToken = await RegisterAndGetTokenAsync(client,
            $"participant-{Guid.NewGuid():N}@example.com", "Participant");
        Authenticate(client, participantToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/events", dto)).StatusCode);
    }

    [Fact]
    public async Task OrganiserCannotEditOrDeleteAnotherOrganisersEvent()
    {
        using var client = CreateClient();
        var first = await RegisterAndGetTokenAsync(client, $"org1-{Guid.NewGuid():N}@example.com", "Organiser");
        var second = await RegisterAndGetTokenAsync(client, $"org2-{Guid.NewGuid():N}@example.com", "Organiser");

        // Create the event as the first organiser.
        var seeded = await _factory.SeedEventAsync();
        Authenticate(client, first);
        var dto = new CreateEventDto("Owned Race", null, DateTime.UtcNow.Date.AddDays(25),
            new TimeSpan(9, 0, 0), seeded.LocationId, 20);
        var create = await client.PostAsJsonAsync("/api/events", dto);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var json = System.Text.Json.JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var eventId = json.RootElement.GetProperty("eventId").GetInt32();

        Authenticate(client, second);
        var update = new UpdateEventDto("Hijacked", null, DateTime.UtcNow.Date.AddDays(25),
            new TimeSpan(9, 0, 0), seeded.LocationId, 20, "Scheduled");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync($"/api/events/{eventId}", update)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.DeleteAsync($"/api/events/{eventId}")).StatusCode);
    }

    [Fact]
    public async Task ParticipantCanEnrol_EnrolmentIsRecorded_AndDuplicateIsRejected()
    {
        using var client = CreateClient();
        var participantToken = await RegisterAndGetTokenAsync(client,
            $"enrol-{Guid.NewGuid():N}@example.com", "Participant");
        var seeded = await _factory.SeedEventAsync();

        Authenticate(client, participantToken);
        var response = await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrol",
            new CreateEnrolmentDto(seeded.CategoryId, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.NotNull(enrolment);
        Assert.Equal(seeded.EventId, enrolment.EventId);
        Assert.Equal(seeded.CategoryId, enrolment.CategoryId);
        Assert.Equal("Active", enrolment.Status);
        Assert.Equal("Pending", enrolment.PaymentStatus);
        Assert.False(string.IsNullOrWhiteSpace(enrolment.EntryNumber));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        Assert.True(await db.Enrolments.AnyAsync(e =>
            e.EnrolmentId == enrolment.EnrolmentId &&
            e.EventId == seeded.EventId &&
            e.CategoryId == seeded.CategoryId &&
            e.UserId == enrolment.UserId));

        var duplicate = await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrol",
            new CreateEnrolmentDto(seeded.CategoryId, null));
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedAndOrganiserCannotEnrolAsParticipant()
    {
        using var client = CreateClient();
        var seeded = await _factory.SeedEventAsync();
        var dto = new CreateEnrolmentDto(seeded.CategoryId, null);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrol", dto)).StatusCode);

        var organiserToken = await RegisterAndGetTokenAsync(client,
            $"organiser-{Guid.NewGuid():N}@example.com", "Organiser");
        Authenticate(client, organiserToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrol", dto)).StatusCode);
    }

    [Fact]
    public async Task ParticipantCannotReadAnotherUsersEnrolment()
    {
        using var client = CreateClient();
        var participantOne = await RegisterAndGetTokenAsync(client,
            $"p1-{Guid.NewGuid():N}@example.com", "Participant");
        var participantTwo = await RegisterAndGetTokenAsync(client,
            $"p2-{Guid.NewGuid():N}@example.com", "Participant");
        var seeded = await _factory.SeedEventAsync();

        Authenticate(client, participantOne);
        var response = await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrol",
            new CreateEnrolmentDto(seeded.CategoryId, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.NotNull(enrolment);

        Authenticate(client, participantTwo);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync($"/api/enrolments/{enrolment.EnrolmentId}")).StatusCode);
    }
}
