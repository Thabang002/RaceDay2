using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Areas.Participant.Controllers;

public class ResultsController : ParticipantControllerBase
{
    public ResultsController(IRaceDayApiClient api) : base(api) { }

    /// <summary>My Results: personal race history across completed events.</summary>
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetMyResultsAsync();
        if (!result.Success) Error(result.Error ?? "We couldn't load your results.");
        return View(new MyResultsViewModel { Results = result.Data ?? new() });
    }
}
