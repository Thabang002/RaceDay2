using System.Globalization;
using Microsoft.AspNetCore.Localization;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

// Same number/date parsing everywhere (local Windows machine or Linux container):
// form values such as "21.1" always use "." as the decimal separator.
CultureInfo.DefaultThreadCurrentCulture = Ui.Culture;
CultureInfo.DefaultThreadCurrentUICulture = Ui.Culture;

var builder = WebApplication.CreateBuilder(args);

// MVC + global filters: anti-forgery on every POST, and graceful handling of API failures.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<ApiExceptionFilter>();

    // Only explicit [Required] attributes count - display-only, non-nullable properties
    // on view models must not make a posted form "invalid".
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// Session = where the logged-in user's JWT, id, name and role live.
var idleMinutes = builder.Configuration.GetValue("Session:IdleTimeoutMinutes", 120);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(idleMinutes);
});
builder.Services.AddHttpContextAccessor();

// Typed HttpClient for the Part 2 API. Base URL comes from config / environment (Api__BaseUrl in Docker).
builder.Services.AddHttpClient<IRaceDayApiClient, RaceDayApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5163/";
    if (!baseUrl.EndsWith('/')) baseUrl += "/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Api:TimeoutSeconds", 30));
});

var app = builder.Build();

// Friendly error pages everywhere - users never see stack traces or raw API errors.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Locally (https profile) this redirects HTTP -> HTTPS. In Docker the app only listens on HTTP 8080,
// no HTTPS port is configured, so the middleware simply does nothing.
app.UseHttpsRedirection();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(Ui.Culture),
    SupportedCultures = new List<CultureInfo> { Ui.Culture },
    SupportedUICultures = new List<CultureInfo> { Ui.Culture }
});

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Each role has its own area (and therefore its own pages and navigation).
app.MapAreaControllerRoute(
    name: "organiser",
    areaName: "Organiser",
    pattern: "Organiser/{controller=Dashboard}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "participant",
    areaName: "Participant",
    pattern: "Participant/{controller=Events}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
