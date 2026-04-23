namespace Donately.Domain.Entities;

public enum VerificationStatus
{
    NotStarted = 0,
    InProgress = 1,
    InReview = 2,
    Approved = 3,
    Rejected = 4,
    NeedsRevision = 5
}

