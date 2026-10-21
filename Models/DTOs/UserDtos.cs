namespace RaceDay.Models.DTOs;

public record UserDto(int UserId, string Email, string FullName, string Role, DateTime CreatedAt);

public record UpdateProfileDto(string FullName, string Email, string? Password);