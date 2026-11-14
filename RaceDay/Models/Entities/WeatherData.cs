using RaceDay.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models.Entities;

public class WeatherData
{
    [Key]
    [Column("weather_id")]
    public int WeatherId { get; set; }

    [Column("event_id")]
    public int EventId { get; set; }

    [Column("forecast_date")]
    public DateTime ForecastDate { get; set; }

    [Column("temperature", TypeName = "decimal(5,2)")]
    public decimal Temperature { get; set; }

    [MaxLength(100)]
    [Column("conditions")]
    public string? Conditions { get; set; }

    [Column("wind_speed", TypeName = "decimal(5,2)")]
    public decimal WindSpeed { get; set; }

    [Column("humidity", TypeName = "decimal(5,2)")]
    public decimal Humidity { get; set; }

    [Column("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }
}