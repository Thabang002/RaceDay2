using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

/// <summary>Outcome of an API call. Errors are already turned into user-friendly text.</summary>
public class ApiResult
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public string? Error { get; init; }

    /// <summary>Field-level validation errors returned by the API (property name -> messages).</summary>
    public Dictionary<string, string[]> FieldErrors { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; init; }
}

/// <summary>Thrown when the API rejects the session token (expired / invalid). Handled by <see cref="ApiExceptionFilter"/>.</summary>
public class ApiUnauthorizedException : Exception
{
    public ApiUnauthorizedException() : base("The API rejected the session token.") { }
}

public interface IRaceDayApiClient
{
    // Auth
    Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();

    // Events
    Task<ApiResult<List<EventModel>>> GetEventsAsync(EventFilter? filter = null);
    Task<ApiResult<List<string>>> GetEventTypesAsync();
    Task<ApiResult<List<OrganiserEventSummary>>> GetMyEventsAsync();
    Task<ApiResult<EventModel>> GetEventAsync(int id);
    Task<ApiResult<int>> CreateEventAsync(EventRequest request);
    Task<ApiResult> UpdateEventAsync(int id, EventRequest request);
    Task<ApiResult> DeleteEventAsync(int id);
    Task<ApiResult> UploadBannerAsync(int eventId, IFormFile file);

    // Categories
    Task<ApiResult<List<CategoryModel>>> GetCategoriesAsync(int eventId);
    Task<ApiResult<CategoryModel>> GetCategoryAsync(int id);
    Task<ApiResult> CreateCategoryAsync(int eventId, CategoryRequest request);
    Task<ApiResult> UpdateCategoryAsync(int id, CategoryRequest request);
    Task<ApiResult> DeleteCategoryAsync(int id);

    // Locations
    Task<ApiResult<List<LocationModel>>> GetLocationsAsync();
    Task<ApiResult<LocationModel>> CreateLocationAsync(LocationRequest request);

    // Enrolments
    Task<ApiResult<List<EnrolmentModel>>> GetEventEnrolmentsAsync(int eventId);
    Task<ApiResult<List<EnrolmentModel>>> GetUserEnrolmentsAsync(int userId);
    Task<ApiResult<EnrolmentModel>> EnrolAsync(int eventId, int categoryId);
    Task<ApiResult> WithdrawAsync(int enrolmentId);
    Task<ApiResult> UpdatePaymentAsync(int enrolmentId, string paymentStatus);

    // Results
    Task<ApiResult<List<ResultEntryModel>>> GetEventResultsAsync(int eventId);
    Task<ApiResult> SaveResultsAsync(int eventId, SaveResultsRequest request);
    Task<ApiResult> PublishResultsAsync(int eventId, bool publish);
    Task<ApiResult<List<MyResultModel>>> GetMyResultsAsync();

    // Profile
    Task<ApiResult<UserModel>> GetProfileAsync();
    Task<ApiResult<UserModel>> UpdateProfileAsync(ProfileRequest request);
    Task<ApiResult<UserModel>> UploadProfilePictureAsync(IFormFile file);

    // Files (images from Azure Blob Storage, streamed through the API)
    Task<HttpResponseMessage?> GetFileAsync(string blobName, CancellationToken ct);
}

/// <summary>
/// The only way the MVC app talks to data: HTTP calls to the RaceDay API.
/// Attaches the session JWT as a Bearer token on every request.
/// </summary>
public class RaceDayApiClient : IRaceDayApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _context;
    private readonly ILogger<RaceDayApiClient> _logger;

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor context, ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _context = context;
        _logger = logger;
    }

    // ------------------------------------------------------------------ Auth
    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", request, authenticated: false);

    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", request, authenticated: false);

    public async Task LogoutAsync()
    {
        // Best effort only: JWTs are stateless, the session is what really ends the login.
        try { await SendAsync(HttpMethod.Post, "api/auth/logout", null, throwOnUnauthorized: false); }
        catch { /* ignore */ }
    }

    // ------------------------------------------------------------------ Events
    public Task<ApiResult<List<EventModel>>> GetEventsAsync(EventFilter? filter = null)
    {
        var q = new List<string>();
        if (filter != null)
        {
            if (filter.Upcoming) q.Add("upcoming=true");
            if (!string.IsNullOrWhiteSpace(filter.Search)) q.Add("search=" + Uri.EscapeDataString(filter.Search.Trim()));
            if (!string.IsNullOrWhiteSpace(filter.EventType)) q.Add("eventType=" + Uri.EscapeDataString(filter.EventType));
            if (filter.LocationId.HasValue) q.Add("locationId=" + filter.LocationId.Value);
            if (filter.From.HasValue) q.Add("from=" + filter.From.Value.ToString("yyyy-MM-dd"));
            if (filter.To.HasValue) q.Add("to=" + filter.To.Value.ToString("yyyy-MM-dd"));
            var preset = EventFilter.DistancePresets.FirstOrDefault(p => p.Key == filter.Distance);
            if (preset.Min.HasValue) q.Add("minDistance=" + preset.Min.Value.ToString(CultureInfo.InvariantCulture));
            if (preset.Max.HasValue) q.Add("maxDistance=" + preset.Max.Value.ToString(CultureInfo.InvariantCulture));
        }
        var url = "api/events" + (q.Count > 0 ? "?" + string.Join("&", q) : string.Empty);
        return SendAsync<List<EventModel>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<List<string>>> GetEventTypesAsync() =>
        SendAsync<List<string>>(HttpMethod.Get, "api/events/types");

    public Task<ApiResult<List<OrganiserEventSummary>>> GetMyEventsAsync() =>
        SendAsync<List<OrganiserEventSummary>>(HttpMethod.Get, "api/events/mine");

    public Task<ApiResult<EventModel>> GetEventAsync(int id) =>
        SendAsync<EventModel>(HttpMethod.Get, $"api/events/{id}");

    public async Task<ApiResult<int>> CreateEventAsync(EventRequest request)
    {
        var result = await SendAsync<JsonElement>(HttpMethod.Post, "api/events", request);
        if (!result.Success) return Fail<int>(result);
        var id = result.Data.TryGetProperty("eventId", out var p) ? p.GetInt32() : 0;
        return new ApiResult<int> { Success = true, StatusCode = result.StatusCode, Data = id };
    }

    public Task<ApiResult> UpdateEventAsync(int id, EventRequest request) =>
        SendAsync(HttpMethod.Put, $"api/events/{id}", request);

    public Task<ApiResult> DeleteEventAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/events/{id}");

    public async Task<ApiResult> UploadBannerAsync(int eventId, IFormFile file) =>
        await UploadAsync<JsonElement>($"api/events/{eventId}/banner", file);

    // ------------------------------------------------------------------ Categories
    public Task<ApiResult<List<CategoryModel>>> GetCategoriesAsync(int eventId) =>
        SendAsync<List<CategoryModel>>(HttpMethod.Get, $"api/events/{eventId}/categories");

    public Task<ApiResult<CategoryModel>> GetCategoryAsync(int id) =>
        SendAsync<CategoryModel>(HttpMethod.Get, $"api/categories/{id}");

    public Task<ApiResult> CreateCategoryAsync(int eventId, CategoryRequest request) =>
        SendAsync(HttpMethod.Post, $"api/events/{eventId}/categories", request);

    public Task<ApiResult> UpdateCategoryAsync(int id, CategoryRequest request) =>
        SendAsync(HttpMethod.Put, $"api/categories/{id}", request);

    public Task<ApiResult> DeleteCategoryAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/categories/{id}");

    // ------------------------------------------------------------------ Locations
    public Task<ApiResult<List<LocationModel>>> GetLocationsAsync() =>
        SendAsync<List<LocationModel>>(HttpMethod.Get, "api/locations");

    public Task<ApiResult<LocationModel>> CreateLocationAsync(LocationRequest request) =>
        SendAsync<LocationModel>(HttpMethod.Post, "api/locations", request);

    // ------------------------------------------------------------------ Enrolments
    public Task<ApiResult<List<EnrolmentModel>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<EnrolmentModel>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    public Task<ApiResult<List<EnrolmentModel>>> GetUserEnrolmentsAsync(int userId) =>
        SendAsync<List<EnrolmentModel>>(HttpMethod.Get, $"api/users/{userId}/enrolments");

    public Task<ApiResult<EnrolmentModel>> EnrolAsync(int eventId, int categoryId) =>
        SendAsync<EnrolmentModel>(HttpMethod.Post, $"api/events/{eventId}/enrol", new EnrolRequest(categoryId, null));

    public Task<ApiResult> WithdrawAsync(int enrolmentId) =>
        SendAsync(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");

    public Task<ApiResult> UpdatePaymentAsync(int enrolmentId, string paymentStatus) =>
        SendAsync(HttpMethod.Put, $"api/enrolments/{enrolmentId}/payment", new PaymentRequest(paymentStatus));

    // ------------------------------------------------------------------ Results
    public Task<ApiResult<List<ResultEntryModel>>> GetEventResultsAsync(int eventId) =>
        SendAsync<List<ResultEntryModel>>(HttpMethod.Get, $"api/events/{eventId}/results");

    public Task<ApiResult> SaveResultsAsync(int eventId, SaveResultsRequest request) =>
        SendAsync(HttpMethod.Put, $"api/events/{eventId}/results", request);

    public Task<ApiResult> PublishResultsAsync(int eventId, bool publish) =>
        SendAsync(HttpMethod.Post, $"api/events/{eventId}/results/publish?publish={(publish ? "true" : "false")}");

    public Task<ApiResult<List<MyResultModel>>> GetMyResultsAsync() =>
        SendAsync<List<MyResultModel>>(HttpMethod.Get, "api/users/me/results");

    // ------------------------------------------------------------------ Profile
    public Task<ApiResult<UserModel>> GetProfileAsync() =>
        SendAsync<UserModel>(HttpMethod.Get, "api/users/profile");

    public Task<ApiResult<UserModel>> UpdateProfileAsync(ProfileRequest request) =>
        SendAsync<UserModel>(HttpMethod.Put, "api/users/profile", request);

    public Task<ApiResult<UserModel>> UploadProfilePictureAsync(IFormFile file) =>
        UploadAsync<UserModel>("api/users/profile/picture", file);

    // ------------------------------------------------------------------ Files
    public async Task<HttpResponseMessage?> GetFileAsync(string blobName, CancellationToken ct)
    {
        try
        {
            var path = string.Join('/', blobName.Split('/').Select(Uri.EscapeDataString));
            var response = await _http.GetAsync($"api/files/{path}", HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.IsSuccessStatusCode) return response;
            response.Dispose();
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Could not load file {Blob} from API", blobName);
            return null;
        }
    }

    // ================================================================== plumbing

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body = null,
        bool authenticated = true, bool throwOnUnauthorized = true) =>
        await SendCoreAsync<object>(method, url, body is null ? null : JsonContent.Create(body, options: Json),
            authenticated, throwOnUnauthorized, readBody: false);

    private Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null, bool authenticated = true) =>
        SendCoreAsync<T>(method, url, body is null ? null : JsonContent.Create(body, options: Json),
            authenticated, throwOnUnauthorized: true, readBody: true);

    private async Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var ct)
            ? ct : new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(file.FileName));
        return await SendCoreAsync<T>(HttpMethod.Post, url, content, authenticated: true, throwOnUnauthorized: true, readBody: true);
    }

    private async Task<ApiResult<T>> SendCoreAsync<T>(HttpMethod method, string url, HttpContent? content,
        bool authenticated, bool throwOnUnauthorized, bool readBody)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        var token = _context.HttpContext?.Session.Token();
        var sentToken = authenticated && !string.IsNullOrEmpty(token);
        if (sentToken) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "RaceDay API unreachable: {Method} {Url}", method, url);
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Error = "We couldn't reach the RaceDay service right now. Please try again in a moment."
            };
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                T? data = default;
                if (readBody && response.StatusCode != HttpStatusCode.NoContent)
                {
                    try
                    {
                        data = await response.Content.ReadFromJsonAsync<T>(Json);
                    }
                    catch (Exception ex) when (ex is JsonException or NotSupportedException)
                    {
                        _logger.LogError(ex, "Unexpected response body from {Url}", url);
                        return new ApiResult<T>
                        {
                            Success = false,
                            StatusCode = HttpStatusCode.InternalServerError,
                            Error = "We received an unexpected response. Please try again."
                        };
                    }
                }
                return new ApiResult<T> { Success = true, StatusCode = response.StatusCode, Data = data };
            }

            // A token we sent was rejected -> the session is no longer valid.
            if (response.StatusCode == HttpStatusCode.Unauthorized && sentToken && throwOnUnauthorized)
                throw new ApiUnauthorizedException();

            var (message, fields) = await ReadErrorAsync(response);
            _logger.LogWarning("API {Method} {Url} returned {Status}: {Message}", method, url, (int)response.StatusCode, message);

            return new ApiResult<T>
            {
                Success = false,
                StatusCode = response.StatusCode,
                Error = message,
                FieldErrors = fields
            };
        }
    }

    private static ApiResult<T> Fail<T>(ApiResult r) => new()
    {
        Success = false, StatusCode = r.StatusCode, Error = r.Error, FieldErrors = r.FieldErrors
    };

    /// <summary>Extracts a friendly message from { message } bodies or ASP.NET ProblemDetails.</summary>
    private static async Task<(string Message, Dictionary<string, string[]> Fields)> ReadErrorAsync(HttpResponseMessage response)
    {
        var fields = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        string? message = null;

        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text) && text.TrimStart().StartsWith('{'))
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                    message = m.GetString();

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in errors.EnumerateObject())
                    {
                        if (prop.Value.ValueKind != JsonValueKind.Array) continue;
                        var key = prop.Name.StartsWith("$.") ? prop.Name[2..] : prop.Name;
                        fields[key] = prop.Value.EnumerateArray()
                            .Where(v => v.ValueKind == JsonValueKind.String)
                            .Select(v => FriendlyValidation(v.GetString()!))
                            .ToArray();
                    }
                    message ??= "Please check the highlighted fields and try again.";
                }
            }
        }
        catch (JsonException)
        {
            // Body wasn't JSON - fall through to the generic messages.
        }

        message ??= response.StatusCode switch
        {
            HttpStatusCode.BadRequest => "Some of the information provided is not valid.",
            HttpStatusCode.Unauthorized => "Invalid email or password.",
            HttpStatusCode.Forbidden => "You don't have permission to do that.",
            HttpStatusCode.NotFound => "We couldn't find what you were looking for.",
            HttpStatusCode.Conflict => "That conflicts with existing data.",
            HttpStatusCode.RequestEntityTooLarge => "That file is too large. Images must be 5 MB or smaller.",
            HttpStatusCode.UnsupportedMediaType => "That file type isn't supported.",
            _ => "Something went wrong on our side. Please try again."
        };

        return (message, fields);
    }

    /// <summary>JSON-converter errors are technical; replace them with something a user understands.</summary>
    private static string FriendlyValidation(string error) =>
        error.Contains("could not be converted", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("JSON", StringComparison.Ordinal)
            ? "This value is not in a valid format."
            : error;
}
