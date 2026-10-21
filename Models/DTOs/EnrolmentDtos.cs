namespace RaceDay.Models.DTOs;

public record CreateEnrolmentDto(int CategoryId, string? PaymentStatus);

public record UpdateEnrolmentDto(string Status);

public record UpdatePaymentDto(string PaymentStatus);

public record EnrolmentDto(
    int EnrolmentId, int UserId, string? UserName,
    int EventId, string? EventTitle,
    int CategoryId, string? CategoryName,
    DateTime RegistrationDate, string Status,
    string? PaymentStatus, string? EntryNumber
);