
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Data;
using RaceDay.Models.Entities;

namespace RaceDay.Tests;

public sealed class RaceDayApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
        $"RaceDayTests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove the application's SQL Server registrations.
            services.RemoveAll<RaceDayDbContext>();
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<RaceDayDbContext>>();

            // Register only the InMemory provider for tests.
            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task<(int LocationId, int EventId, int CategoryId)>
        SeedEventAsync(int organiserId = 1, string title = "Seeded Race")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<RaceDayDbContext>();

        await db.Database.EnsureCreatedAsync();

        var location = new Location
        {
            Name = "Test Stadium",
            City = "Polokwane",
            Province = "Limpopo"
        };

        db.Locations.Add(location);
        await db.SaveChangesAsync();

        var ev = new Event
        {
            Title = title,
            Description = "Test event",
            Date = DateTime.UtcNow.Date.AddDays(30),
            StartTime = new TimeSpan(9, 0, 0),
            LocationId = location.LocationId,
            MaxParticipants = 100,
            CreatedBy = organiserId
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var category = new Category
        {
            EventId = ev.EventId,
            Name = "10 km",
            Description = "10 kilometre race",
            DistanceKm = 10,
            StartFee = 100,
            MaxParticipants = 20
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return (location.LocationId, ev.EventId, category.CategoryId);
    }
}
