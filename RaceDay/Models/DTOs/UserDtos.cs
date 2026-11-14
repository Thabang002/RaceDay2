using System.ComponentModel.DataAnnotations;

namespace RaceDay.Models.DTOs;

public record UserDto(
    int UserId, string Email, string FullName, string Role, DateTime CreatedAt,
    string? PhoneNumber, DateTime? DateOfBirth, string? Gender, string? City,
    string? ProfilePicture
);

public record UpdateProfileDto(
    [Required, StringLength(255)] string FullName,
    [Required, EmailAddress] string Email,
    [MinLength(6)] string? Password,
    [Phone, StringLength(30)] string? PhoneNumber = null,
    DateTime? DateOfBirth = null,
    [StringLength(20)] string? Gender = null,
    [StringLength(100)] string? City = null
);

public static class UserMapping
{
    public static UserDto ToDto(this Entities.User u) => new(
        u.UserId, u.Email, u.FullName, u.Role, u.CreatedAt,
        u.PhoneNumber, u.DateOfBirth, u.Gender, u.City, u.ProfilePicture);
}
