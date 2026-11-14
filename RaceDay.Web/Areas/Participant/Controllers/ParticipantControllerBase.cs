using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Services;

namespace RaceDay.Web.Areas.Participant.Controllers;

[Area("Participant")]
[SessionAuthorize(Roles = Roles.Participant)]
public abstract class ParticipantControllerBase : RaceDayControllerBase
{
    protected readonly IRaceDayApiClient Api;
    protected ParticipantControllerBase(IRaceDayApiClient api) => Api = api;
}
