using RaceDay.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models.Entities;

public class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required, MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    [Column("role")]
    public string Role { get; set; } = "Participant"; // "Organiser" | "Participant"

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ---- Part 3 additions ----
    [MaxLength(30)]
    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    [Column("gender")]
    public string? Gender { get; set; }

    [MaxLength(100)]
    [Column("city")]
    public string? City { get; set; }

    /// <summary>Blob name of the profile picture in Azure Blob Storage (e.g. profiles/abc.jpg).</summary>
    [MaxLength(300)]
    [Column("profile_picture")]
    public string? ProfilePicture { get; set; }

    // Navigation
    public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    public ICollection<EventOrganiser> EventOrganisers { get; set; } = new List<EventOrganiser>();
}