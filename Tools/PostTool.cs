using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using VerintCsharpMcp.Models;
using VerintCsharpMcp.Services;

namespace VerintCsharpMcp.Tools;

[McpServerToolType]
public class PostTool
{
    [McpServerTool]
    [Description(
        "Draft a NEW forum thread in Verint Community. " +
        "Use this tool only when the user explicitly wants to post/create a thread in a FORUM. " +
        "The thread is not posted immediately. A confirmation is required."
    )]
    public static async Task<string> DraftCreateForumThread(
        PendingActionStore store,
        VerintClient verintClient,

        [Description("Forum name where the new thread must be created.")]
        string forumName,

        [Description("Title/subject of the new forum thread.")]
        string title,

        [Description("Body/content of the new forum thread.")]
        string body,

        [Description("Optional group name if multiple forums have the same name.")]
        string? groupName = null,

        [Description("Optional forum ID. If supplied, it must belong to the specified forum name.")]
        int? forumId = null)
    {
        if (string.IsNullOrWhiteSpace(forumName))
            return "Forum name is required.";

        if (string.IsNullOrWhiteSpace(title))
            return "Thread title is required.";

        if (string.IsNullOrWhiteSpace(body))
            return "Thread body is required.";

        var forumResult = await ResolveForum(
            verintClient,
            forumName.Trim(),
            groupName?.Trim(),
            forumId);

        if (!forumResult.Success)
            return forumResult.Message;

        var action = new PendingAction
        {
            ActionType = ActionType.Create,

            // IMPORTANT: explicitly set the target
            Target = ActionTarget.Forum,

            ForumId = forumResult.Forum!.ForumId,

            Title = title.Trim(),
            Body = body
        };

        store.Add(action);

        return $"""
        FORUM THREAD DRAFT CREATED

        Action ID: {action.ActionId}

        Target: Forum
        Forum: {forumResult.Forum.ForumName}
        Forum ID: {forumResult.Forum.ForumId}
        Group: {forumResult.Forum.GroupName ?? "N/A"}

        Thread Title:
        {action.Title}

        Thread Body:
        {action.Body}

        The thread has NOT been posted yet.

        Explicit confirmation is required.
        Use the confirmation tool with Action ID:
        {action.ActionId}
        """;
    }


    [McpServerTool]
    [Description(
        "Draft an EDIT to an existing forum thread. " +
        "Use only for forum threads. The thread is not changed until confirmation."
    )]
    public static async Task<string> DraftEditForumThread(
        PendingActionStore store,
        VerintClient verintClient,

        [Description("Forum name containing the thread.")]
        string forumName,

        [Description("Current thread title/name.")]
        string threadName,

        [Description("New thread title.")]
        string title,

        [Description("New thread body.")]
        string body,

        [Description("Optional group name.")]
        string? groupName = null,

        [Description("Optional forum ID.")]
        int? forumId = null,

        [Description("Optional thread ID.")]
        int? threadId = null)
    {
        if (string.IsNullOrWhiteSpace(forumName))
            return "Forum name is required.";

        if (string.IsNullOrWhiteSpace(threadName))
            return "Thread name is required.";

        if (string.IsNullOrWhiteSpace(title))
            return "New thread title is required.";

        if (string.IsNullOrWhiteSpace(body))
            return "New thread body is required.";

        var forumResult = await ResolveForum(
            verintClient,
            forumName.Trim(),
            groupName?.Trim(),
            forumId);

        if (!forumResult.Success)
            return forumResult.Message;

        var threadResult = await ResolveThread(
            verintClient,
            forumResult.Forum!.ForumId,
            threadName.Trim(),
            threadId);

        if (!threadResult.Success)
            return threadResult.Message;

        var action = new PendingAction
        {
            ActionType = ActionType.Edit,

            // IMPORTANT
            Target = ActionTarget.Forum,

            ForumId = forumResult.Forum.ForumId,
            ThreadId = threadResult.Thread!.ThreadId,

            Title = title.Trim(),
            Body = body
        };

        store.Add(action);

        return $"""
        FORUM THREAD EDIT DRAFT CREATED

        Action ID: {action.ActionId}

        Target: Forum
        Forum: {forumResult.Forum.ForumName}
        Forum ID: {forumResult.Forum.ForumId}

        Thread:
        {threadResult.Thread.Title}

        Thread ID:
        {threadResult.Thread.ThreadId}

        New Title:
        {action.Title}

        New Body:
        {action.Body}

        The thread has NOT been changed yet.

        Explicit confirmation is required.
        Action ID:
        {action.ActionId}
        """;
    }


    [McpServerTool]
    [Description(
        "Draft deletion of an existing forum thread. " +
        "Use only for forum threads. The thread is not deleted until confirmation."
    )]
    public static async Task<string> DraftDeleteForumThread(
        PendingActionStore store,
        VerintClient verintClient,

        [Description("Forum name containing the thread.")]
        string forumName,

        [Description("Thread name/title.")]
        string threadName,

        [Description("Optional group name.")]
        string? groupName = null,

        [Description("Optional forum ID.")]
        int? forumId = null,

        [Description("Optional thread ID.")]
        int? threadId = null)
    {
        if (string.IsNullOrWhiteSpace(forumName))
            return "Forum name is required.";

        if (string.IsNullOrWhiteSpace(threadName))
            return "Thread name is required.";

        var forumResult = await ResolveForum(
            verintClient,
            forumName.Trim(),
            groupName?.Trim(),
            forumId);

        if (!forumResult.Success)
            return forumResult.Message;

        var threadResult = await ResolveThread(
            verintClient,
            forumResult.Forum!.ForumId,
            threadName.Trim(),
            threadId);

        if (!threadResult.Success)
            return threadResult.Message;

        var action = new PendingAction
        {
            ActionType = ActionType.Delete,

            // IMPORTANT
            Target = ActionTarget.Forum,

            ForumId = forumResult.Forum.ForumId,
            ThreadId = threadResult.Thread!.ThreadId,

            Title = threadResult.Thread.Title
        };

        store.Add(action);

        return $"""
        FORUM THREAD DELETE DRAFT CREATED

        Action ID: {action.ActionId}

        Target: Forum
        Forum: {forumResult.Forum.ForumName}
        Forum ID: {forumResult.Forum.ForumId}

        Thread:
        {threadResult.Thread.Title}

        Thread ID:
        {threadResult.Thread.ThreadId}

        The thread has NOT been deleted.

        Explicit confirmation is required.
        Action ID:
        {action.ActionId}
        """;
    }


    // ============================================================
    // FORUM RESOLUTION
    // ============================================================

    private static async Task<ForumResolution> ResolveForum(
        VerintClient verintClient,
        string forumName,
        string? groupName,
        int? forumId)
    {
        var forums = await ForumTool.GetAllForums(
            verintClient,
            forumName);

        if (forums.Count == 0)
        {
            return ForumResolution.Fail(
                $"NO FORUM FOUND\n\nForum: {forumName}");
        }

        IEnumerable<ForumTool.ForumMatch> matches = forums;

        if (!string.IsNullOrWhiteSpace(groupName))
        {
            matches = matches.Where(x =>
                string.Equals(
                    x.GroupName,
                    groupName,
                    StringComparison.OrdinalIgnoreCase));

            var groupMatches = matches.ToList();

            if (groupMatches.Count == 0)
            {
                return ForumResolution.Fail(
                    $"NO FORUM FOUND IN GROUP\n\n" +
                    $"Forum: {forumName}\n" +
                    $"Group: {groupName}");
            }

            matches = groupMatches;
        }

        var list = matches.ToList();

        if (forumId.HasValue)
        {
            var selected = list.FirstOrDefault(x =>
                x.ForumId == forumId.Value);

            if (selected == null)
            {
                return ForumResolution.Fail(
                    $"FORUM ID NOT FOUND\n\n" +
                    $"Forum: {forumName}\n" +
                    $"Forum ID: {forumId.Value}");
            }

            return ForumResolution.SuccessResult(selected);
        }

        var distinctForums = list
            .GroupBy(x => x.ForumId)
            .Select(x => x.First())
            .ToList();

        if (distinctForums.Count > 1)
        {
            var response =
                "MULTIPLE FORUMS FOUND\n\n" +
                $"Forum name: {forumName}\n\n";

            foreach (var forum in distinctForums)
            {
                response +=
                    $"Forum ID: {forum.ForumId}\n" +
                    $"Forum: {forum.ForumName}\n" +
                    $"Group: {forum.GroupName ?? "N/A"}\n\n";
            }

            response +=
                "Please provide the Forum ID or Group Name.";

            return ForumResolution.Fail(response);
        }

        return ForumResolution.SuccessResult(distinctForums[0]);
    }


    private static async Task<ThreadResolution> ResolveThread(
        VerintClient verintClient,
        int forumId,
        string threadName,
        int? threadId)
    {
        var threads = await GetThreads(
            verintClient,
            forumId);

        if (threadId.HasValue)
        {
            var selected = threads.FirstOrDefault(x =>
                x.ThreadId == threadId.Value);

            if (selected == null)
            {
                return ThreadResolution.Fail(
                    $"THREAD ID NOT FOUND\n\n" +
                    $"Thread ID: {threadId.Value}\n" +
                    $"Forum ID: {forumId}");
            }

            if (!string.Equals(
                    selected.Title,
                    threadName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return ThreadResolution.Fail(
                    $"THREAD NAME DOES NOT MATCH THREAD ID\n\n" +
                    $"Expected: {threadName}\n" +
                    $"Actual: {selected.Title}\n" +
                    $"Thread ID: {threadId.Value}");
            }

            return ThreadResolution.SuccessResult(selected);
        }

        var matches = threads
            .Where(x =>
                string.Equals(
                    x.Title,
                    threadName,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return ThreadResolution.Fail(
                $"NO THREAD FOUND\n\n" +
                $"Thread: {threadName}\n" +
                $"Forum ID: {forumId}");
        }

        if (matches.Count > 1)
        {
            var response =
                $"MULTIPLE THREADS FOUND\n\n" +
                $"Thread name: {threadName}\n\n";

            foreach (var thread in matches)
            {
                response +=
                    $"Thread ID: {thread.ThreadId}\n" +
                    $"Title: {thread.Title}\n\n";
            }

            response += "Please provide the Thread ID.";

            return ThreadResolution.Fail(response);
        }

        return ThreadResolution.SuccessResult(matches[0]);
    }


    private static async Task<List<ThreadMatch>> GetThreads(
        VerintClient verintClient,
        int forumId)
    {
        var result = new List<ThreadMatch>();

        int pageIndex = 0;
        const int pageSize = 100;

        while (true)
        {
            var url =
                $"api.ashx/v2/forums/{forumId}/threads.json" +
                $"?PageIndex={pageIndex}" +
                $"&PageSize={pageSize}";

            var json = await verintClient.GetAsync(url);

            using var document =
                JsonDocument.Parse(json);

            var root = document.RootElement;

            if (!root.TryGetProperty(
                    "Threads",
                    out var threadsElement))
            {
                break;
            }

            var returnedCount = 0;

            foreach (var thread in threadsElement.EnumerateArray())
            {
                returnedCount++;

                if (!thread.TryGetProperty(
                        "Id",
                        out var idElement))
                    continue;

                var id = idElement.GetInt32();

                string title = string.Empty;

                if (thread.TryGetProperty(
                        "Subject",
                        out var subjectElement))
                {
                    title = subjectElement.GetString() ?? string.Empty;
                }
                else if (thread.TryGetProperty(
                        "Title",
                        out var titleElement))
                {
                    title = titleElement.GetString() ?? string.Empty;
                }

                result.Add(new ThreadMatch
                {
                    ThreadId = id,
                    Title = title
                });
            }

            if (returnedCount < pageSize)
                break;

            pageIndex++;
        }

        return result;
    }


    private class ThreadMatch
    {
        public int ThreadId { get; set; }
        public string Title { get; set; } = string.Empty;
    }


    private class ForumResolution
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public ForumTool.ForumMatch? Forum { get; set; }

        public static ForumResolution SuccessResult(
            ForumTool.ForumMatch forum)
        {
            return new ForumResolution
            {
                Success = true,
                Forum = forum
            };
        }

        public static ForumResolution Fail(
            string message)
        {
            return new ForumResolution
            {
                Success = false,
                Message = message
            };
        }
    }


    private class ThreadResolution
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public ThreadMatch? Thread { get; set; }

        public static ThreadResolution SuccessResult(
            ThreadMatch thread)
        {
            return new ThreadResolution
            {
                Success = true,
                Thread = thread
            };
        }

        public static ThreadResolution Fail(
            string message)
        {
            return new ThreadResolution
            {
                Success = false,
                Message = message
            };
        }
    }
}





// using System.ComponentModel;
// using System.Text.Json;
// using ModelContextProtocol.Server;
// using VerintCsharpMcp.Models;
// using VerintCsharpMcp.Services;

// namespace VerintCsharpMcp.Tools;

// [McpServerToolType]
// public class PostTool
// {
//     [McpServerTool]
//     [Description(
//         "Create a draft for a new Verint forum post. " +
//         "The post is NOT created until the user explicitly confirms the action.")]
//     public static async Task<string> DraftCreatePost(
//         PendingActionStore store,
//         VerintClient verintClient,

//         [Description(
//             "Verint forum name.")]
//         string forumName,

//         [Description(
//             "Title of the new forum post.")]
//         string title,

//         [Description(
//             "Body of the new forum post.")]
//         string body,

//         [Description(
//             "Optional Verint forum ID. " +
//             "Use this when multiple forums have the same name.")]
//         int? forumId = null)
//     {
//         if (string.IsNullOrWhiteSpace(forumName))
//         {
//             return "DRAFT CREATE FAILED: Forum name is required.";
//         }

//         if (string.IsNullOrWhiteSpace(title))
//         {
//             return "DRAFT CREATE FAILED: Title is required.";
//         }

//         if (string.IsNullOrWhiteSpace(body))
//         {
//             return "DRAFT CREATE FAILED: Body is required.";
//         }

//         var forumResult =
//             await ResolveForum(
//                 verintClient,
//                 forumName,
//                 forumId);

//         if (!forumResult.Success)
//         {
//             return forumResult.Message;
//         }

//         var action =
//             new PendingAction
//             {
//                 ActionType = ActionType.Create,
//                 ForumId = forumResult.Forum!.ForumId,
//                 Title = title,
//                 Body = body
//             };

//         store.Add(action);

//         return BuildCreateDraftResponse(
//             action,
//             forumResult.Forum);
//     }

//     [McpServerTool]
//     [Description(
//         "Create a draft for editing an existing Verint forum thread. " +
//         "Thread name is normally used to find the thread. " +
//         "Thread ID is optional and can be supplied to identify a specific thread. " +
//         "The edit is NOT executed until the user explicitly confirms the action.")]
//     public static async Task<string> DraftEditPost(
//         PendingActionStore store,
//         VerintClient verintClient,

//         [Description(
//             "Verint forum name.")]
//         string forumName,

//         [Description(
//             "Verint thread name/title.")]
//         string threadName,

//         [Description(
//             "New thread title.")]
//         string title,

//         [Description(
//             "New thread body.")]
//         string body,

//         [Description(
//             "Optional Verint forum ID. " +
//             "Use this when multiple forums have the same name.")]
//         int? forumId = null,

//         [Description(
//             "Optional Verint thread ID. " +
//             "If supplied, it must match the specified thread name and forum.")]
//         int? threadId = null)
//     {
//         if (string.IsNullOrWhiteSpace(forumName))
//         {
//             return "DRAFT EDIT FAILED: Forum name is required.";
//         }

//         if (string.IsNullOrWhiteSpace(threadName))
//         {
//             return "DRAFT EDIT FAILED: Thread name is required.";
//         }

//         if (string.IsNullOrWhiteSpace(title))
//         {
//             return "DRAFT EDIT FAILED: New title is required.";
//         }

//         if (string.IsNullOrWhiteSpace(body))
//         {
//             return "DRAFT EDIT FAILED: New body is required.";
//         }

//         var threadResult =
//             await ResolveThread(
//                 verintClient,
//                 forumName,
//                 threadName,
//                 forumId,
//                 threadId);

//         if (!threadResult.Success)
//         {
//             return threadResult.Message;
//         }

//         var action =
//             new PendingAction
//             {
//                 ActionType = ActionType.Edit,
//                 ForumId = threadResult.Forum!.ForumId,
//                 ThreadId = threadResult.Thread!.ThreadId,
//                 Title = title,
//                 Body = body
//             };

//         store.Add(action);

//         return BuildEditDraftResponse(
//             action,
//             threadResult.Forum,
//             threadResult.Thread);
//     }

//     [McpServerTool]
//     [Description(
//         "Create a draft for deleting an existing Verint forum thread. " +
//         "Thread name is normally used to find the thread. " +
//         "Thread ID is optional and can be supplied to identify a specific thread. " +
//         "The thread is NOT deleted until the user explicitly confirms the action.")]
//     public static async Task<string> DraftDeletePost(
//         PendingActionStore store,
//         VerintClient verintClient,

//         [Description(
//             "Verint forum name.")]
//         string forumName,

//         [Description(
//             "Verint thread name/title.")]
//         string threadName,

//         [Description(
//             "Optional Verint forum ID. " +
//             "Use this when multiple forums have the same name.")]
//         int? forumId = null,

//         [Description(
//             "Optional Verint thread ID. " +
//             "If supplied, it must match the specified thread name and forum.")]
//         int? threadId = null)
//     {
//         if (string.IsNullOrWhiteSpace(forumName))
//         {
//             return "DRAFT DELETE FAILED: Forum name is required.";
//         }

//         if (string.IsNullOrWhiteSpace(threadName))
//         {
//             return "DRAFT DELETE FAILED: Thread name is required.";
//         }

//         var threadResult =
//             await ResolveThread(
//                 verintClient,
//                 forumName,
//                 threadName,
//                 forumId,
//                 threadId);

//         if (!threadResult.Success)
//         {
//             return threadResult.Message;
//         }

//         var action =
//             new PendingAction
//             {
//                 ActionType = ActionType.Delete,
//                 ForumId = threadResult.Forum!.ForumId,
//                 ThreadId = threadResult.Thread!.ThreadId,
//                 Title = threadResult.Thread.Title
//             };

//         store.Add(action);

//         return BuildDeleteDraftResponse(
//             action,
//             threadResult.Forum,
//             threadResult.Thread);
//     }

//     private static async Task<ForumResolution> ResolveForum(
//         VerintClient verintClient,
//         string forumName,
//         int? forumId)
//     {
//         var forums =
//             await ForumTool.GetAllForums(
//                 verintClient,
//                 forumName.Trim());

//         if (forums.Count == 0)
//         {
//             return ForumResolution.Failed(
//                 $"""
//                 NO FORUM FOUND

//                 No forum named "{forumName}" was found.

//                 No action was created.
//                 """);
//         }

//         if (forumId.HasValue)
//         {
//             var matchingForum =
//                 forums.FirstOrDefault(
//                     forum =>
//                         forum.ForumId == forumId.Value);

//             if (matchingForum == null)
//             {
//                 return ForumResolution.Failed(
//                     $"""
//                     FORUM SELECTION INVALID

//                     Forum name:
//                     {forumName}

//                     Forum ID:
//                     {forumId.Value}

//                     The supplied forum ID does not belong to a forum
//                     named "{forumName}".

//                     No action was created.
//                     """);
//             }

//             return ForumResolution.Successful(
//                 matchingForum);
//         }

//         if (forums.Count > 1)
//         {
//             var options =
//                 string.Join(
//                     Environment.NewLine,
//                     forums.Select(
//                         (forum, index) =>
//                             $"{index + 1}. " +
//                             $"Forum ID: {forum.ForumId}, " +
//                             $"Forum: {forum.ForumName}, " +
//                             $"Group: {forum.GroupName ?? "N/A"}"));

//             return ForumResolution.Failed(
//                 $"""
//                 MULTIPLE FORUMS FOUND

//                 More than one forum named "{forumName}" exists.

//                 Please choose one:

//                 {options}

//                 No action was created.

//                 Call the tool again with the selected forumId.
//                 """);
//         }

//         return ForumResolution.Successful(
//             forums[0]);
//     }

//     private static async Task<ThreadResolution> ResolveThread(
//         VerintClient verintClient,
//         string forumName,
//         string threadName,
//         int? forumId,
//         int? threadId)
//     {
//         var forumResult =
//             await ResolveForum(
//                 verintClient,
//                 forumName,
//                 forumId);

//         if (!forumResult.Success)
//         {
//             return ThreadResolution.Failed(
//                 forumResult.Message);
//         }

//         var forum =
//             forumResult.Forum!;

//         if (threadId.HasValue)
//         {
//             var thread =
//                 await FindThreadById(
//                     verintClient,
//                     forum.ForumId,
//                     threadId.Value);

//             if (thread == null)
//             {
//                 return ThreadResolution.Failed(
//                     $"""
//                     THREAD NOT FOUND

//                     Forum:
//                     {forum.ForumName}

//                     Forum ID:
//                     {forum.ForumId}

//                     Thread ID:
//                     {threadId.Value}

//                     No action was created.
//                     """);
//             }

//             if (!string.Equals(
//                     thread.Title,
//                     threadName.Trim(),
//                     StringComparison.OrdinalIgnoreCase))
//             {
//                 return ThreadResolution.Failed(
//                     $"""
//                     THREAD SELECTION INVALID

//                     Forum:
//                     {forum.ForumName}

//                     Supplied thread name:
//                     {threadName}

//                     Supplied thread ID:
//                     {threadId.Value}

//                     Actual thread name:
//                     {thread.Title}

//                     The supplied thread ID does not match
//                     the supplied thread name.

//                     No action was created.
//                     """);
//             }

//             return ThreadResolution.Successful(
//                 forum,
//                 thread);
//         }

//         var threads =
//             await FindThreadsByName(
//                 verintClient,
//                 forum.ForumId,
//                 threadName.Trim());

//         if (threads.Count == 0)
//         {
//             return ThreadResolution.Failed(
//                 $"""
//                 THREAD NOT FOUND

//                 Forum:
//                 {forum.ForumName}

//                 Thread:
//                 {threadName}

//                 No matching thread was found.

//                 No action was created.
//                 """);
//         }

//         if (threads.Count > 1)
//         {
//             var options =
//                 string.Join(
//                     Environment.NewLine,
//                     threads.Select(
//                         (thread, index) =>
//                             $"{index + 1}. " +
//                             $"Thread ID: {thread.ThreadId}, " +
//                             $"Thread: {thread.Title}"));

//             return ThreadResolution.Failed(
//                 $"""
//                 MULTIPLE THREADS FOUND

//                 More than one thread named "{threadName}"
//                 exists in the forum "{forum.ForumName}".

//                 Please choose one:

//                 {options}

//                 No action was created.

//                 Call the tool again with the selected threadId.
//                 """);
//         }

//         return ThreadResolution.Successful(
//             forum,
//             threads[0]);
//     }

//     private static async Task<ThreadMatch?> FindThreadById(
//         VerintClient verintClient,
//         int forumId,
//         int threadId)
//     {
//         var threads =
//             await GetThreads(
//                 verintClient,
//                 forumId);

//         return threads.FirstOrDefault(
//             thread =>
//                 thread.ThreadId == threadId);
//     }

//     private static async Task<List<ThreadMatch>> FindThreadsByName(
//         VerintClient verintClient,
//         int forumId,
//         string threadName)
//     {
//         var threads =
//             await GetThreads(
//                 verintClient,
//                 forumId);

//         return threads
//             .Where(
//                 thread =>
//                     string.Equals(
//                         thread.Title,
//                         threadName,
//                         StringComparison.OrdinalIgnoreCase))
//             .ToList();
//     }

//     private static async Task<List<ThreadMatch>> GetThreads(
//         VerintClient verintClient,
//         int forumId)
//     {
//         var matches =
//             new List<ThreadMatch>();

//         var pageIndex = 0;

//         const int pageSize = 100;

//         while (true)
//         {
//             var url =
//                 $"api.ashx/v2/forums/{forumId}/threads.json" +
//                 $"?PageIndex={pageIndex}" +
//                 $"&PageSize={pageSize}";

//             var response =
//                 await verintClient.GetAsync(url);

//             using var document =
//                 JsonDocument.Parse(response);

//             if (!document.RootElement.TryGetProperty(
//                     "Threads",
//                     out var threadsElement))
//             {
//                 break;
//             }

//             foreach (var thread in threadsElement.EnumerateArray())
//             {
//                 var id =
//                     GetIntProperty(
//                         thread,
//                         "Id");

//                 var title =
//                     GetStringProperty(
//                         thread,
//                         "Subject")
//                     ?? GetStringProperty(
//                         thread,
//                         "Title");

//                 if (id <= 0 ||
//                     string.IsNullOrWhiteSpace(title))
//                 {
//                     continue;
//                 }

//                 matches.Add(
//                     new ThreadMatch
//                     {
//                         ThreadId = id,
//                         Title = title
//                     });
//             }

//             var totalCount =
//                 GetIntProperty(
//                     document.RootElement,
//                     "TotalCount");

//             var returnedCount =
//                 threadsElement.GetArrayLength();

//             if (returnedCount == 0 ||
//                 (totalCount > 0 &&
//                  (pageIndex + 1) * pageSize >= totalCount))
//             {
//                 break;
//             }

//             if (returnedCount < pageSize)
//             {
//                 break;
//             }

//             pageIndex++;
//         }

//         return matches;
//     }

//     private static int GetIntProperty(
//         JsonElement element,
//         string propertyName)
//     {
//         if (!element.TryGetProperty(
//                 propertyName,
//                 out var property))
//         {
//             return 0;
//         }

//         if (property.ValueKind ==
//             JsonValueKind.Number)
//         {
//             return property.GetInt32();
//         }

//         if (property.ValueKind ==
//             JsonValueKind.String &&
//             int.TryParse(
//                 property.GetString(),
//                 out var value))
//         {
//             return value;
//         }

//         return 0;
//     }

//     private static string? GetStringProperty(
//         JsonElement element,
//         string propertyName)
//     {
//         if (!element.TryGetProperty(
//                 propertyName,
//                 out var property))
//         {
//             return null;
//         }

//         return property.ValueKind ==
//                JsonValueKind.String
//             ? property.GetString()
//             : null;
//     }

//     private static string BuildCreateDraftResponse(
//         PendingAction action,
//         ForumTool.ForumMatch forum)
//     {
//         return $"""
//         CREATE POST DRAFT

//         Action ID:
//         {action.ActionId}

//         Forum:
//         {forum.ForumName}

//         Forum ID:
//         {forum.ForumId}

//         Title:
//         {action.Title}

//         Body:
//         {action.Body}

//         This is only a draft.

//         No post has been created in Verint.

//         Explicit user confirmation is required before execution.
//         """;
//     }

//     private static string BuildEditDraftResponse(
//         PendingAction action,
//         ForumTool.ForumMatch forum,
//         ThreadMatch thread)
//     {
//         return $"""
//         EDIT POST DRAFT

//         Action ID:
//         {action.ActionId}

//         Forum:
//         {forum.ForumName}

//         Forum ID:
//         {forum.ForumId}

//         Thread:
//         {thread.Title}

//         Thread ID:
//         {thread.ThreadId}

//         New Title:
//         {action.Title}

//         New Body:
//         {action.Body}

//         This is only a draft.

//         No changes have been made in Verint.

//         Explicit user confirmation is required before execution.
//         """;
//     }

//     private static string BuildDeleteDraftResponse(
//         PendingAction action,
//         ForumTool.ForumMatch forum,
//         ThreadMatch thread)
//     {
//         return $"""
//         DELETE POST DRAFT

//         Action ID:
//         {action.ActionId}

//         Forum:
//         {forum.ForumName}

//         Forum ID:
//         {forum.ForumId}

//         Thread:
//         {thread.Title}

//         Thread ID:
//         {thread.ThreadId}

//         This is only a draft.

//         The thread has NOT been deleted.

//         Explicit user confirmation is required before execution.
//         """;
//     }

//     private class ThreadMatch
//     {
//         public int ThreadId { get; set; }

//         public string Title { get; set; } = "";
//     }

//     private class ForumResolution
//     {
//         public bool Success { get; set; }

//         public string Message { get; set; } = "";

//         public ForumTool.ForumMatch? Forum { get; set; }

//         public static ForumResolution Successful(
//             ForumTool.ForumMatch forum)
//         {
//             return new ForumResolution
//             {
//                 Success = true,
//                 Forum = forum
//             };
//         }

//         public static ForumResolution Failed(
//             string message)
//         {
//             return new ForumResolution
//             {
//                 Success = false,
//                 Message = message
//             };
//         }
//     }

//     private class ThreadResolution
//     {
//         public bool Success { get; set; }

//         public string Message { get; set; } = "";

//         public ForumTool.ForumMatch? Forum { get; set; }

//         public ThreadMatch? Thread { get; set; }

//         public static ThreadResolution Successful(
//             ForumTool.ForumMatch forum,
//             ThreadMatch thread)
//         {
//             return new ThreadResolution
//             {
//                 Success = true,
//                 Forum = forum,
//                 Thread = thread
//             };
//         }

//         public static ThreadResolution Failed(
//             string message)
//         {
//             return new ThreadResolution
//             {
//                 Success = false,
//                 Message = message
//             };
//         }
//     }
// }
