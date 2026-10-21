using RaceDay.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models.Entities;

public class Category
{
    [Key]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("event_id")]
    public int EventId { get; set; }

    [Required, MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("distance_km", TypeName = "decimal(6,2)")]
    public decimal DistanceKm { get; set; }

    [Column("start_fee", TypeName = "decimal(10,2)")]
    public decimal StartFee { get; set; }

    [Column("max_participants")]
    public int MaxParticipants { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}