namespace Donately.Domain.Entities;

public enum VerificationRequestStatus
{
    NotStarted = 0,
    InProgress = 1,
    InReview = 2,
    Approved = 3,
    Rejected = 4,
    NeedsRevision = 5
}

