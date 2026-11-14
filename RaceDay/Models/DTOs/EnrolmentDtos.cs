namespace RaceDay.Models.DTOs;

public record CreateEnrolmentDto(int CategoryId, string? PaymentStatus);

public record UpdateEnrolmentDto(string Status);

public record UpdatePaymentDto(string PaymentStatus);

/// <param name="DisplayStatus">
/// Single status for the UI: "Confirmed" (active + paid), "Pending" (active, awaiting payment/confirmation)
/// or the raw status when the enrolment is no longer active (e.g. "Withdrawn").
/// </param>
public record EnrolmentDto(
    int EnrolmentId, int UserId, string? UserName,
    int EventId, string? EventTitle,
    int CategoryId, string? CategoryName,
    DateTime RegistrationDate, string Status,
    string? PaymentStatus, string? EntryNumber,
    DateTime? EventDate,
    string? DisplayStatus,
    string? UserEmail
);

public static class EnrolmentStatus
{
    public const string Active = "Active";
    public const string Confirmed = "Confirmed";
    public const string Pending = "Pending";

    public static string Display(string status, string? paymentStatus)
    {
        if (!string.Equals(status, Active, StringComparison.OrdinalIgnoreCase)) return status;
        return paymentStatus is "Paid" or "Confirmed" ? Confirmed : Pending;
    }
}
