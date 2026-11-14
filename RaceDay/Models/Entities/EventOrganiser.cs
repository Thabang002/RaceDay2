using RaceDay.Models.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models.Entities;

public class EventOrganiser
{
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("event_id")]
    public int EventId { get; set; }

    [Column("role")]
    public string? Role { get; set; }

    public User? User { get; set; }
    public Event? Event { get; set; }
}