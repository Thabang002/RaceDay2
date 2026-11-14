using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Web.Models;

namespace RaceDay.Web.ViewModels;

public class EventBrowseViewModel
{
    public EventFilter Filter { get; set; } = new();
    public List<EventModel> Events { get; set; } = new();
    public List<string> EventTypes { get; set; } = new();
    public List<SelectListItem> Locations { get; set; } = new();
    public HashSet<int> EnrolledEventIds { get; set; } = new();
    public string ParticipantName { get; set; } = string.Empty;
}

public class EventDetailsViewModel
{
    public EventModel Event { get; set; } = new();
    public List<CategoryModel> Categories { get; set; } = new();
    public EnrolmentModel? MyEnrolment { get; set; }
    public int? SelectedCategoryId { get; set; }

    public bool EnrolmentOpen => Event.IsUpcoming && Event.Status is not ("Cancelled" or "Completed" or "Closed");
    public bool CanEnrol => EnrolmentOpen && MyEnrolment == null && Categories.Any(c => !c.IsFull);
}

public class MyEnrolmentsViewModel
{
    public List<EnrolmentModel> Upcoming { get; set; } = new();
    public List<EnrolmentModel> Past { get; set; } = new();
}

public class MyResultsViewModel
{
    public List<MyResultModel> Results { get; set; } = new();

    public int RacesCompleted => Results.Count(r => r.FinishTime.HasValue);
    public int? BestPosition => Results.Where(r => r.Position.HasValue).Select(r => r.Position).Min();
    public decimal TotalKm => Results.Where(r => r.FinishTime.HasValue).Sum(r => r.DistanceKm ?? 0);
}
