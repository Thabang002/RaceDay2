using RaceDay.Models.DTOs;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models.DTOs;

public record RegisterDto(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] string FullName,
    [Required] string Role // "Organiser" or "Participant"
);

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponseDto(string Token, UserDto User);