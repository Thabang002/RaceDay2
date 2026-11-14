using RaceDay.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Models.Entities;

public class Result
{
    [Key]
    [Column("result_id")]
    public int ResultId { get; set; }

    [Column("enrolment_id")]
    public int EnrolmentId { get; set; }

    [Column("finish_time")]
    public TimeSpan? FinishTime { get; set; }

    [Column("position")]
    public int? Position { get; set; }

    [Column("pace_per_km", TypeName = "decimal(6,2)")]
    public decimal? PacePerKm { get; set; }

    [MaxLength(50)]
    [Column("status")]
    public string? Status { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [ForeignKey(nameof(EnrolmentId))]
    public Enrolment? Enrolment { get; set; }
}