using lc.Models.Enums;

namespace lc.Models;

public sealed class AuthorRequest
{
    public int RequestId { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public string Message { get; set; } = string.Empty;

    public AuthorRequestStatus Status { get; set; } = AuthorRequestStatus.Pending;

    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public int? ReviewerId { get; set; }
    public User? Reviewer { get; set; }

    public string? ReviewComment { get; set; }
}