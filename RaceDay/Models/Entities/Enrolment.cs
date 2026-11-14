using RaceDay.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models.Entities;

public class Enrolment
{
    [Key]
    [Column("enrolment_id")]
    public int EnrolmentId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("event_id")]
    public int EventId { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("registration_date")]
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    [Column("status")]
    public string Status { get; set; } = "Active";

    [MaxLength(50)]
    [Column("payment_status")]
    public string? PaymentStatus { get; set; } = "Pending";

    [MaxLength(50)]
    [Column("entry_number")]
    public string? EntryNumber { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event? Event { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public Result? Result { get; set; }
}