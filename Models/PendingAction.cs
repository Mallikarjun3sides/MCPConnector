namespace VerintCsharpMcp.Models;

public enum ActionType
{
    Create,
    Edit,
    Delete
}

public enum ActionTarget 
{ 
    Forum, 
    Blog 
}

public class PendingAction
{
    public string ActionId { get; set; } =
        Guid.NewGuid().ToString();

    public ActionType ActionType { get; set; }
    public ActionTarget Target { get; set; } = 
        ActionTarget.Forum;

    public int? ForumId { get; set; }
    public int? GroupId { get; set; }
    public int? ThreadId { get; set; }
    public int? BlogId { get; set; }
    public int? BlogPostId { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } =
        DateTime.UtcNow.AddMinutes(15);
    public bool Confirmed { get; set; }
}