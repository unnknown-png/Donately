namespace Donately.Domain.Entities;

public enum VerificationRequestStatus
{
    NotStarted = 0,
    InReview = 1,
    Approved = 2,
    Rejected = 3,
    NeedsRevision = 4
}

