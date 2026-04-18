namespace Donately.Domain.Entities;

public class Donation
{
    public Guid Id { get; set; }

    public Guid FundraiserId { get; set; }

    public Fundraiser Fundraiser { get; set; } = null!;

    public Guid DonorId { get; set; }

    public ApplicationUser Donor { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "UAH";

    public bool Anonymous { get; set; }

    public DonationStatus Status { get; set; } = DonationStatus.Pending;

    public Guid? PaymentTransactionId { get; set; }

    public PaymentTransaction? PaymentTransaction { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
}

