using RaceDay.Models.Entities;

namespace RaceDay.Services
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
