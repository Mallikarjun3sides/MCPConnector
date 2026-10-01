using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using VerintCsharpMcp.Models;
using VerintCsharpMcp.Services;

namespace VerintCsharpMcp.Tools;

[McpServerToolType]
public class ConfirmationTool
{
    [McpServerTool]
    [Description(
        "Confirm and execute a previously created Verint Community draft action. " +
        "The action must have been created by a forum or blog draft tool. " +
        "Use this only after the user explicitly confirms the action."
    )]
    public static async Task<string> ConfirmAction(
        PendingActionStore store,
        VerintClient verintClient,

        [Description("Action ID returned by the draft tool.")]
        string actionId,

        [Description(
            "Must be true only when the user has explicitly confirmed " +
            "that the draft should be executed."
        )]
        bool confirmed)
    {
        if (string.IsNullOrWhiteSpace(actionId))
            return "Action ID is required.";

        if (!confirmed)
        {
            return """
            CONFIRMATION REQUIRED

            The action was NOT executed.

            The user must explicitly confirm the pending action.
            """;
        }

        if (!store.TryClaim(actionId, out var action))
        {
            return $"""
            ACTION NOT FOUND OR ALREADY EXECUTED

            Action ID:
            {actionId}
            """;
        }

        if (action.ExpiresAt <= DateTime.UtcNow)
        {
            return $"""
            ACTION EXPIRED

            Action ID:
            {actionId}

            Please create a new draft.
            """;
        }

        try
        {
            string result;

            if (action.Target == ActionTarget.Forum)
            {
                result = await ExecuteForumAction(
                    verintClient,
                    action);
            }
            else if (action.Target == ActionTarget.Blog)
            {
                result = await ExecuteBlogAction(
                    verintClient,
                    action);
            }
            else
            {
                return "Unsupported action target.";
            }

            action.Confirmed = true;

            return result;
        }
        catch (Exception ex)
        {
            // Put the action back so it can be retried.
            store.Add(action);

            return $"""
            ACTION EXECUTION FAILED

            Action ID:
            {action.ActionId}

            Target:
            {action.Target}

            Error:
            {ex.Message}

            The action was not successfully executed.
            """;
        }
    }


    // ============================================================
    // FORUM
    // ============================================================

    private static async Task<string> ExecuteForumAction(
        VerintClient verintClient,
        PendingAction action)
    {
        if (!action.ForumId.HasValue)
            throw new Exception(
                "Forum ID is missing.");

        return action.ActionType switch
        {
            ActionType.Create =>
                await CreateForumThread(
                    verintClient,
                    action),

            ActionType.Edit =>
                await EditForumThread(
                    verintClient,
                    action),

            ActionType.Delete =>
                await DeleteForumThread(
                    verintClient,
                    action),

            _ =>
                throw new Exception(
                    "Unsupported forum action.")
        };
    }


    private static async Task<string> CreateForumThread(
        VerintClient verintClient,
        PendingAction action)
    {
        var forumId = action.ForumId!.Value;

        var payload = new
        {
            ForumId = forumId,
            Subject = action.Title,
            Body = action.Body
        };

        var json = JsonSerializer.Serialize(payload);

        var response =
            await verintClient.PostAsync(
                $"api.ashx/v2/forums/{forumId}/threads.json",
                json);

        return $"""
        FORUM THREAD CREATED SUCCESSFULLY

        Action ID:
        {action.ActionId}

        Forum ID:
        {forumId}

        Thread Title:
        {action.Title}

        Response:
        {response}
        """;
    }


    private static async Task<string> EditForumThread(
        VerintClient verintClient,
        PendingAction action)
    {
        if (!action.ThreadId.HasValue)
            throw new Exception(
                "Thread ID is missing.");

        var forumId = action.ForumId!.Value;
        var threadId = action.ThreadId!.Value;

        var payload = new
        {
            ForumId = forumId,
            ThreadId = threadId,
            Subject = action.Title,
            Body = action.Body
        };

        var json = JsonSerializer.Serialize(payload);

        var response =
            await verintClient.PutAsync(
                $"api.ashx/v2/forums/{forumId}/threads/{threadId}.json",
                json);

        return $"""
        FORUM THREAD UPDATED SUCCESSFULLY

        Action ID:
        {action.ActionId}

        Forum ID:
        {forumId}

        Thread ID:
        {threadId}

        New Title:
        {action.Title}

        Response:
        {response}
        """;
    }


    // private static async Task<string> DeleteForumThread(
    //     VerintClient verintClient,
    //     PendingAction action)
    // {
    //     if (!action.ThreadId.HasValue)
    //         throw new Exception(
    //             "Thread ID is missing.");

    //     var forumId = action.ForumId!.Value;
    //     var threadId = action.ThreadId!.Value;

    //     var response =
    //         await verintClient.DeleteAsync(
    //             $"api.ashx/v2/forums/{forumId}/threads/{threadId}.json");

    //     return $"""
    //     FORUM THREAD DELETED SUCCESSFULLY

    //     Action ID:
    //     {action.ActionId}

    //     Forum ID:
    //     {forumId}

    //     Thread ID:
    //     {threadId}

    //     Response:
    //     {response}
    //     """;
    // }

    private static async Task<string> DeleteForumThread(
    VerintClient verintClient,
    PendingAction action)
{
    if (!action.ThreadId.HasValue)
        throw new Exception(
            "Thread ID is missing.");

    var forumId = action.ForumId!.Value;
    var threadId = action.ThreadId!.Value;

    var payload = new
    {
        ForumId = forumId,
        ThreadId = threadId
    };

    var json = JsonSerializer.Serialize(payload);

    var response =
        await verintClient.DeleteAsync(
            $"api.ashx/v2/forums/{forumId}/threads/{threadId}.json",
            json);

    return $"""
    FORUM THREAD DELETED SUCCESSFULLY

    Action ID:
    {action.ActionId}

    Forum ID:
    {forumId}

    Thread ID:
    {threadId}

    Response:
    {response}
    """;
}


    // ============================================================
    // BLOG
    // ============================================================

    private static async Task<string> ExecuteBlogAction(
        VerintClient verintClient,
        PendingAction action)
    {
        if (!action.BlogId.HasValue)
            throw new Exception(
                "Blog ID is missing.");

        return action.ActionType switch
        {
            ActionType.Create =>
                await CreateBlogPost(
                    verintClient,
                    action),

            ActionType.Edit =>
                await EditBlogPost(
                    verintClient,
                    action),

            ActionType.Delete =>
                await DeleteBlogPost(
                    verintClient,
                    action),

            _ =>
                throw new Exception(
                    "Unsupported blog action.")
        };
    }


    private static async Task<string> CreateBlogPost(
        VerintClient verintClient,
        PendingAction action)
    {
        var blogId = action.BlogId!.Value;

        var payload = new
        {
            BlogId = blogId,
            Title = action.Title,
            Body = action.Body
        };

        var json = JsonSerializer.Serialize(payload);

        var response =
            await verintClient.PostAsync(
                $"api.ashx/v2/blogs/{blogId}/posts.json",
                json);

        return $"""
        BLOG POST CREATED SUCCESSFULLY

        Action ID:
        {action.ActionId}

        Blog ID:
        {blogId}

        Title:
        {action.Title}

        Response:
        {response}
        """;
    }


    private static async Task<string> EditBlogPost(
        VerintClient verintClient,
        PendingAction action)
    {
        if (!action.BlogPostId.HasValue)
            throw new Exception(
                "Blog Post ID is missing.");

        var blogId = action.BlogId!.Value;
        var postId = action.BlogPostId!.Value;

        var payload = new
        {
            BlogId = blogId,
            Id = postId,
            Title = action.Title,
            Body = action.Body
        };

        var json = JsonSerializer.Serialize(payload);

        var response =
            await verintClient.PutAsync(
                $"api.ashx/v2/blogs/{blogId}/posts/{postId}.json",
                json);

        return $"""
        BLOG POST UPDATED SUCCESSFULLY

        Action ID:
        {action.ActionId}

        Blog ID:
        {blogId}

        Post ID:
        {postId}

        New Title:
        {action.Title}

        Response:
        {response}
        """;
    }


    // private static async Task<string> DeleteBlogPost(
    //     VerintClient verintClient,
    //     PendingAction action)
    // {
    //     if (!action.BlogPostId.HasValue)
    //         throw new Exception(
    //             "Blog Post ID is missing.");

    //     var blogId = action.BlogId!.Value;
    //     var postId = action.BlogPostId!.Value;

    //     var response =
    //         await verintClient.DeleteAsync(
    //             $"api.ashx/v2/blogs/{blogId}/posts/{postId}.json");

    //     return $"""
    //     BLOG POST DELETED SUCCESSFULLY

    //     Action ID:
    //     {action.ActionId}

    //     Blog ID:
    //     {blogId}

    //     Post ID:
    //     {postId}

    //     Response:
    //     {response}
    //     """;
    // }

    private static async Task<string> DeleteBlogPost(
    VerintClient verintClient,
    PendingAction action)
{
    if (!action.BlogPostId.HasValue)
        throw new Exception(
            "Blog Post ID is missing.");

    var blogId = action.BlogId!.Value;
    var postId = action.BlogPostId!.Value;

    var payload = new
    {
        BlogId = blogId,
        Id = postId
    };

    var json = JsonSerializer.Serialize(payload);

    var response =
        await verintClient.DeleteAsync(
            $"api.ashx/v2/blogs/{blogId}/posts/{postId}.json",
            json);

    return $"""
    BLOG POST DELETED SUCCESSFULLY

    Action ID:
    {action.ActionId}

    Blog ID:
    {blogId}

    Post ID:
    {postId}

    Response:
    {response}
    """;
}
}