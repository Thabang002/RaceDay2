using RaceDay.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models.Entities;

public class Event
{
    [Key]
    [Column("event_id")]
    public int EventId { get; set; }

    [Required, MaxLength(255)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("start_time")]
    public TimeSpan StartTime { get; set; }

    [Column("location_id")]
    public int LocationId { get; set; }

    [Column("max_participants")]
    public int MaxParticipants { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "Scheduled";

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    // Navigation
    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public User? Organiser { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    public ICollection<EventOrganiser> EventOrganisers { get; set; } = new List<EventOrganiser>();
    public ICollection<WeatherData> WeatherData { get; set; } = new List<WeatherData>();
}