using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Web.Models;

namespace RaceDay.Web.ViewModels;

public class OrganiserDashboardViewModel
{
    public string OrganiserName { get; set; } = string.Empty;
    public List<OrganiserEventSummary> Events { get; set; } = new();

    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public int ConfirmedEnrolments => Events.Sum(e => e.ConfirmedCount);
    public List<OrganiserEventSummary> Upcoming => Events.Where(e => e.IsUpcoming && e.Status != "Cancelled")
        .OrderBy(e => e.Date).ThenBy(e => e.StartTime).ToList();
    public List<OrganiserEventSummary> AwaitingResults => Events
        .Where(e => !e.IsUpcoming && !e.ResultsPublished && e.Status != "Cancelled").ToList();
}

public class EventFormViewModel : IValidatableObject
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 255 characters.")]
    [Display(Name = "Event name")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please add a short description.")]
    [StringLength(4000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Please choose a start time.")]
    [DataType(DataType.Time)]
    [Display(Name = "Start time")]
    public TimeSpan? StartTime { get; set; }

    [Required(ErrorMessage = "Please choose a location.")]
    [Display(Name = "Location")]
    public int? LocationId { get; set; }

    [Required(ErrorMessage = "Please enter the race distance.")]
    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string? EventType { get; set; }

    [Required(ErrorMessage = "Please enter the participant limit.")]
    [Range(1, 100000, ErrorMessage = "Max participants must be between 1 and 100 000.")]
    [Display(Name = "Max participants")]
    public int? MaxParticipants { get; set; }

    public string? Status { get; set; }

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBanner { get; set; }

    // dropdown data
    public List<SelectListItem> Locations { get; set; } = new();
    public List<string> EventTypes { get; set; } = new();

    public bool IsEdit => EventId.HasValue;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsEdit && Date.HasValue && Date.Value.Date < DateTime.Today)
            yield return new ValidationResult("New events can't be scheduled in the past.", new[] { nameof(Date) });

        if (Banner != null)
        {
            var ext = Path.GetExtension(Banner.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif"))
                yield return new ValidationResult("Banner must be a JPG, PNG, WEBP or GIF image.", new[] { nameof(Banner) });
            if (Banner.Length > 5 * 1024 * 1024)
                yield return new ValidationResult("Banner must be 5 MB or smaller.", new[] { nameof(Banner) });
        }
    }

    public static readonly string[] Statuses = { "Scheduled", "Open", "Closed", "Completed", "Cancelled" };
}

public class EventManageViewModel
{
    public EventModel Event { get; set; } = new();
    public List<CategoryModel> Categories { get; set; } = new();
    public int EnrolmentCount { get; set; }
    public int ConfirmedCount { get; set; }
}

public class CategoryFormViewModel
{
    public int? CategoryId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter a category name.")]
    [StringLength(255, MinimumLength = 2)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please enter the distance.")]
    [Range(0, 1000, ErrorMessage = "Distance must be between 0 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please enter the entry fee (0 for free).")]
    [Range(0, 100000, ErrorMessage = "Fee must be between R0 and R100 000.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? StartFee { get; set; }

    [Required(ErrorMessage = "Please enter the participant limit.")]
    [Range(1, 100000, ErrorMessage = "Max participants must be between 1 and 100 000.")]
    [Display(Name = "Max participants")]
    public int? MaxParticipants { get; set; }
}

public class CategoriesPageViewModel
{
    public EventModel Event { get; set; } = new();
    public List<CategoryModel> Categories { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class LocationFormViewModel
{
    [Required(ErrorMessage = "Please enter the venue name.")]
    [StringLength(255)]
    [Display(Name = "Venue name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Please enter the city or town.")]
    [StringLength(100)]
    [Display(Name = "City / town")]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Province { get; set; }

    [StringLength(20)]
    [Display(Name = "Postal code")]
    public string? PostalCode { get; set; }

    public string? ReturnUrl { get; set; }

    public static readonly string[] Provinces =
    {
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
        "Mpumalanga", "North West", "Northern Cape", "Western Cape"
    };
}

public class EnrolmentsPageViewModel
{
    public EventModel Event { get; set; } = new();
    public List<EnrolmentModel> Enrolments { get; set; } = new();
    public List<CategoryModel> Categories { get; set; } = new();
    public int? CategoryFilter { get; set; }
    public string? StatusFilter { get; set; }

    public IEnumerable<EnrolmentModel> Filtered => Enrolments
        .Where(e => !CategoryFilter.HasValue || e.CategoryId == CategoryFilter)
        .Where(e => string.IsNullOrEmpty(StatusFilter) || e.StatusLabel == StatusFilter);
}

public class ResultsEntryViewModel
{
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
    public EventModel Event { get; set; } = new();
    public List<ResultRowInput> Rows { get; set; } = new();
    public bool AutoRank { get; set; }

    public bool CanCapture => Event.Date.Date <= DateTime.Today;
    public int CapturedCount => Rows.Count(r => !string.IsNullOrWhiteSpace(r.FinishTime) || r.Position.HasValue || !string.IsNullOrWhiteSpace(r.Status));

    public static readonly string[] Statuses = { "Finished", "DNF", "DNS", "DSQ" };
}

public class ResultRowInput
{
    public int EnrolmentId { get; set; }
    public string? ParticipantName { get; set; }
    public string? EntryNumber { get; set; }
    public string? CategoryName { get; set; }
    public decimal? PacePerKm { get; set; }

    [RegularExpression(@"^\s*\d{1,2}:[0-5]\d:[0-5]\d\s*$", ErrorMessage = "Use h:mm:ss, e.g. 1:45:30")]
    public string? FinishTime { get; set; }

    [Range(1, 100000, ErrorMessage = "Position must be 1 or more.")]
    public int? Position { get; set; }

    public string? Status { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
