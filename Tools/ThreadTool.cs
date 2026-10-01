using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using VerintCsharpMcp.Services;

namespace VerintCsharpMcp.Tools;

[McpServerToolType]
public class ThreadTool
{
    [McpServerTool]
    [Description(
        "Read a specific Verint forum thread by forum name and thread name. " +
        "Forum ID and thread ID are optional and can be used to disambiguate or " +
        "directly identify the forum and thread.")]
    public static async Task<string> ReadThread(
        VerintClient verintClient,

        [Description(
            "Verint forum name, for example 'Nature'.")]
        string forumName,

        [Description(
            "Verint thread name/title.")]
        string threadName,

        [Description(
            "Optional Verint forum ID. " +
            "If supplied, it must belong to the specified forum name.")]
        int? forumId = null,

        [Description(
            "Optional Verint thread ID. " +
            "If supplied, the thread will be verified against the specified forum and thread name.")]
        int? threadId = null)
    {
        if (string.IsNullOrWhiteSpace(forumName))
        {
            return "READ THREAD FAILED: Forum name is required.";
        }

        if (string.IsNullOrWhiteSpace(threadName))
        {
            return "READ THREAD FAILED: Thread name is required.";
        }

        if (forumId.HasValue && forumId.Value <= 0)
        {
            return "READ THREAD FAILED: Forum ID must be greater than zero.";
        }

        if (threadId.HasValue && threadId.Value <= 0)
        {
            return "READ THREAD FAILED: Thread ID must be greater than zero.";
        }

        try
        {
            var forums =
                await ForumTool.GetAllForums(
                    verintClient,
                    forumName.Trim());

            if (forums.Count == 0)
            {
                return $"""
                NO FORUM FOUND

                No forum named "{forumName}" was found.

                No thread was read.
                """;
            }

            ForumTool.ForumMatch selectedForum;

            if (forumId.HasValue)
            {
                var matchingForum =
                    forums.FirstOrDefault(
                        forum =>
                            forum.ForumId == forumId.Value);

                if (matchingForum == null)
                {
                    return $"""
                    FORUM SELECTION INVALID

                    Forum name:
                    {forumName}

                    Forum ID:
                    {forumId.Value}

                    The supplied forum ID does not belong to a forum
                    named "{forumName}".

                    No thread was read.
                    """;
                }

                selectedForum = matchingForum;
            }
            else
            {
                if (forums.Count > 1)
                {
                    return BuildForumSelectionResponse(
                        forumName,
                        forums);
                }

                selectedForum = forums[0];
            }

            int selectedThreadId;

            if (threadId.HasValue)
            {
                var matchingThread =
                    await FindThreadById(
                        verintClient,
                        selectedForum.ForumId,
                        threadId.Value);

                if (matchingThread == null)
                {
                    return $"""
                    THREAD NOT FOUND

                    Forum:
                    {selectedForum.ForumName}

                    Forum ID:
                    {selectedForum.ForumId}

                    Thread ID:
                    {threadId.Value}

                    The supplied thread ID was not found in this forum.

                    No thread was read.
                    """;
                }

                if (!string.Equals(
                        matchingThread.Title,
                        threadName.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return $"""
                    THREAD SELECTION INVALID

                    Forum:
                    {selectedForum.ForumName}

                    Supplied thread name:
                    {threadName}

                    Supplied thread ID:
                    {threadId.Value}

                    The supplied thread ID belongs to a different thread:

                    Actual thread name:
                    {matchingThread.Title}

                    No thread was read.
                    """;
                }

                selectedThreadId = threadId.Value;
            }
            else
            {
                var threads =
                    await FindThreadsByName(
                        verintClient,
                        selectedForum.ForumId,
                        threadName.Trim());

                if (threads.Count == 0)
                {
                    return $"""
                    THREAD NOT FOUND

                    Forum:
                    {selectedForum.ForumName}

                    Thread:
                    {threadName}

                    No matching thread was found.

                    No thread was read.
                    """;
                }

                if (threads.Count > 1)
                {
                    return BuildThreadSelectionResponse(
                        selectedForum,
                        threadName,
                        threads);
                }

                selectedThreadId =
                    threads[0].ThreadId;
            }

            return await verintClient.GetAsync(
                $"api.ashx/v2/forums/{selectedForum.ForumId}/threads/{selectedThreadId}.json");
        }
        catch (Exception ex)
        {
            return $"""
            READ THREAD ERROR

            Type:
            {ex.GetType().Name}

            Message:
            {ex.Message}
            """;
        }
    }

    private static async Task<List<ThreadMatch>> FindThreadsByName(
        VerintClient verintClient,
        int forumId,
        string threadName)
    {
        var matches =
            new List<ThreadMatch>();

        var pageIndex = 0;

        const int pageSize = 100;

        while (true)
        {
            var url =
                $"api.ashx/v2/forums/{forumId}/threads.json" +
                $"?PageIndex={pageIndex}" +
                $"&PageSize={pageSize}";

            var response =
                await verintClient.GetAsync(url);

            using var document =
                JsonDocument.Parse(response);

            if (!document.RootElement.TryGetProperty(
                    "Threads",
                    out var threadsElement))
            {
                break;
            }

            foreach (var thread in threadsElement.EnumerateArray())
            {
                var title =
                    GetStringProperty(
                        thread,
                        "Subject")
                    ?? GetStringProperty(
                        thread,
                        "Title");

                var id =
                    GetIntProperty(
                        thread,
                        "Id");

                if (id <= 0 ||
                    string.IsNullOrWhiteSpace(title))
                {
                    continue;
                }

                if (string.Equals(
                        title,
                        threadName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(
                        new ThreadMatch
                        {
                            ThreadId = id,
                            Title = title
                        });
                }
            }

            var totalCount =
                GetIntProperty(
                    document.RootElement,
                    "TotalCount");

            var returnedCount =
                threadsElement.GetArrayLength();

            if (returnedCount == 0 ||
                (totalCount > 0 &&
                 (pageIndex + 1) * pageSize >= totalCount))
            {
                break;
            }

            if (returnedCount < pageSize)
            {
                break;
            }

            pageIndex++;
        }

        return matches;
    }

    private static async Task<ThreadMatch?> FindThreadById(
        VerintClient verintClient,
        int forumId,
        int threadId)
    {
        var threads =
            await FindThreadsById(
                verintClient,
                forumId,
                threadId);

        return threads.FirstOrDefault();
    }

    private static async Task<List<ThreadMatch>> FindThreadsById(
        VerintClient verintClient,
        int forumId,
        int threadId)
    {
        var matches =
            new List<ThreadMatch>();

        var pageIndex = 0;

        const int pageSize = 100;

        while (true)
        {
            var url =
                $"api.ashx/v2/forums/{forumId}/threads.json" +
                $"?PageIndex={pageIndex}" +
                $"&PageSize={pageSize}";

            var response =
                await verintClient.GetAsync(url);

            using var document =
                JsonDocument.Parse(response);

            if (!document.RootElement.TryGetProperty(
                    "Threads",
                    out var threadsElement))
            {
                break;
            }

            foreach (var thread in threadsElement.EnumerateArray())
            {
                var id =
                    GetIntProperty(
                        thread,
                        "Id");

                if (id != threadId)
                {
                    continue;
                }

                var title =
                    GetStringProperty(
                        thread,
                        "Subject")
                    ?? GetStringProperty(
                        thread,
                        "Title")
                    ?? "";

                matches.Add(
                    new ThreadMatch
                    {
                        ThreadId = id,
                        Title = title
                    });

                return matches;
            }

            var totalCount =
                GetIntProperty(
                    document.RootElement,
                    "TotalCount");

            var returnedCount =
                threadsElement.GetArrayLength();

            if (returnedCount == 0 ||
                (totalCount > 0 &&
                 (pageIndex + 1) * pageSize >= totalCount))
            {
                break;
            }

            if (returnedCount < pageSize)
            {
                break;
            }

            pageIndex++;
        }

        return matches;
    }

    private static int GetIntProperty(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var property))
        {
            return 0;
        }

        if (property.ValueKind ==
            JsonValueKind.Number)
        {
            return property.GetInt32();
        }

        if (property.ValueKind ==
            JsonValueKind.String &&
            int.TryParse(
                property.GetString(),
                out var value))
        {
            return value;
        }

        return 0;
    }

    private static string? GetStringProperty(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var property))
        {
            return null;
        }

        return property.ValueKind ==
               JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static string BuildForumSelectionResponse(
        string forumName,
        List<ForumTool.ForumMatch> forums)
    {
        var options =
            string.Join(
                Environment.NewLine,
                forums.Select(
                    (forum, index) =>
                        $"{index + 1}. " +
                        $"Forum ID: {forum.ForumId}, " +
                        $"Forum: {forum.ForumName}, " +
                        $"Group ID: {forum.GroupId?.ToString() ?? "N/A"}, " +
                        $"Group: {forum.GroupName ?? "N/A"}"));

        return $"""
        MULTIPLE FORUMS FOUND

        More than one forum named "{forumName}" exists.

        Please choose the forum you want to use:

        {options}

        No thread was read.

        After choosing a forum, call this tool again with the
        selected forumId.
        """;
    }

    private static string BuildThreadSelectionResponse(
        ForumTool.ForumMatch forum,
        string threadName,
        List<ThreadMatch> threads)
    {
        var options =
            string.Join(
                Environment.NewLine,
                threads.Select(
                    (thread, index) =>
                        $"{index + 1}. " +
                        $"Thread ID: {thread.ThreadId}, " +
                        $"Thread: {thread.Title}"));

        return $"""
        MULTIPLE THREADS FOUND

        More than one thread named "{threadName}" exists
        in the forum "{forum.ForumName}".

        Please choose the thread you want to use:

        {options}

        No thread was read.

        After choosing a thread, call this tool again with
        the selected threadId.
        """;
    }

    private class ThreadMatch
    {
        public int ThreadId { get; set; }

        public string Title { get; set; } = "";
    }
}




// using System.ComponentModel;
// using System.Text.Json;
// using ModelContextProtocol.Server;
// using VerintCsharpMcp.Services;

// namespace VerintCsharpMcp.Tools;

// [McpServerToolType]
// public class ThreadTool
// {
//     [McpServerTool]
//     [Description(
//         "Read a specific Verint forum thread. " +
//         "Provide the forum name and thread ID. " +
//         "Forum ID is optional and can be used to select a specific forum " +
//         "when multiple forums have the same name.")]
//     public static async Task<string> ReadThread(
//         VerintClient verintClient,

//         [Description(
//             "Verint forum name, for example 'Nature'.")]
//         string forumName,

//         [Description(
//             "Verint thread ID.")]
//         int threadId,

//         [Description(
//             "Optional Verint forum ID. " +
//             "If supplied, it must belong to the specified forum name.")]
//         int? forumId = null)
//     {
//         if (string.IsNullOrWhiteSpace(forumName))
//         {
//             return "READ THREAD FAILED: Forum name is required.";
//         }

//         if (threadId <= 0)
//         {
//             return "READ THREAD FAILED: Thread ID must be greater than zero.";
//         }

//         try
//         {
//             var forums =
//                 await ForumTool.GetAllForums(
//                     verintClient,
//                     forumName.Trim());

//             if (forums.Count == 0)
//             {
//                 return $"""
//                 NO FORUM FOUND

//                 No forum named "{forumName}" was found.

//                 Please provide another forum name.
//                 """;
//             }

//             ForumTool.ForumMatch selectedForum;

//             if (forumId.HasValue)
//             {
//                 selectedForum =
//                     forums.FirstOrDefault(
//                         forum =>
//                             forum.ForumId == forumId.Value);

//                 if (selectedForum == null)
//                 {
//                     return $"""
//                     FORUM SELECTION INVALID

//                     Forum name:
//                     {forumName}

//                     Forum ID:
//                     {forumId.Value}

//                     The supplied forum ID does not belong to a forum
//                     named "{forumName}".

//                     No thread was read.
//                     """;
//                 }
//             }
//             else
//             {
//                 if (forums.Count > 1)
//                 {
//                     return BuildForumSelectionResponse(
//                         forumName,
//                         forums);
//                 }

//                 selectedForum = forums[0];
//             }

//             var response =
//                 await verintClient.GetAsync(
//                     $"api.ashx/v2/forums/{selectedForum.ForumId}/threads/{threadId}.json");

//             return response;
//         }
//         catch (Exception ex)
//         {
//             return $"""
//             READ THREAD ERROR

//             Type:
//             {ex.GetType().Name}

//             Message:
//             {ex.Message}
//             """;
//         }
//     }

//     private static string BuildForumSelectionResponse(
//         string forumName,
//         List<ForumTool.ForumMatch> forums)
//     {
//         var options =
//             string.Join(
//                 Environment.NewLine,
//                 forums.Select(
//                     (forum, index) =>
//                         $"{index + 1}. Forum ID: {forum.ForumId}, " +
//                         $"Forum: {forum.ForumName}, " +
//                         $"Group ID: {forum.GroupId?.ToString() ?? "N/A"}, " +
//                         $"Group: {forum.GroupName ?? "N/A"}"));

//         return $"""
//         MULTIPLE FORUMS FOUND

//         More than one forum named "{forumName}" exists.

//         Please choose the forum you want to use:

//         {options}

//         No thread was read.

//         After choosing a forum, call this tool again with the
//         selected forumId.
//         """;
//     }
// }

