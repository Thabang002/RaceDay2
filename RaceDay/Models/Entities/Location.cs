using RaceDay.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models.Entities;

public class Location
{
    [Key]
    [Column("location_id")]
    public int LocationId { get; set; }

    [Required, MaxLength(255)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("address")]
    public string? Address { get; set; }

    [MaxLength(100)]
    [Column("city")]
    public string? City { get; set; }

    [MaxLength(100)]
    [Column("province")]
    public string? Province { get; set; }

    [MaxLength(20)]
    [Column("postal_code")]
    public string? PostalCode { get; set; }

    [Column("latitude", TypeName = "decimal(9,6)")]
    public decimal? Latitude { get; set; }

    [Column("longitude", TypeName = "decimal(9,6)")]
    public decimal? Longitude { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}