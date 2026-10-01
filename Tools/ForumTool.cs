using System.Text.Json;
using ModelContextProtocol.Server;
using System.ComponentModel;
using VerintCsharpMcp.Services;

namespace VerintCsharpMcp.Tools;

[McpServerToolType]
public class ForumTool
{
    [McpServerTool]
    [Description(
        "Find Verint forums by forum name. " +
        "Returns forum ID and group name. " +
        "Use this when the user wants to select a forum by name.")]
    public static async Task<string> FindForums(
        VerintClient verintClient,
        [Description("Forum name to search for")]
        string forumName)
    {
        if (string.IsNullOrWhiteSpace(forumName))
        {
            return "Forum name cannot be empty.";
        }

        try
        {
            var forums =
                await GetAllForums(
                    verintClient,
                    forumName.Trim());

            if (forums.Count == 0)
            {
                return $"""
                NO FORUM FOUND

                No forum named "{forumName}" was found.

                Please provide another forum name.
                """;
            }

            return JsonSerializer.Serialize(
                new
                {
                    Count = forums.Count,
                    Forums = forums
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });
        }
        catch (Exception ex)
        {
            return $"""
            FIND FORUMS ERROR

            Type:
            {ex.GetType().Name}

            Message:
            {ex.Message}
            """;
        }
    }

    internal static async Task<List<ForumMatch>> GetAllForums(
        VerintClient verintClient,
        string forumName)
    {
        var matches =
            new List<ForumMatch>();

        var pageIndex = 0;

        const int pageSize = 100;

        while (true)
        {
            var url =
                $"api.ashx/v2/forums.json" +
                $"?PageIndex={pageIndex}" +
                $"&PageSize={pageSize}";

            var response =
                await verintClient.GetAsync(url);

            using var document =
                JsonDocument.Parse(response);

            if (!document.RootElement.TryGetProperty(
                    "Forums",
                    out var forumsElement))
            {
                break;
            }

            foreach (var forum in forumsElement.EnumerateArray())
            {
                var name =
                    forum.TryGetProperty(
                        "Name",
                        out var nameElement)
                        ? nameElement.GetString()
                        : null;

                if (!string.Equals(
                        name,
                        forumName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var id =
                    forum.TryGetProperty(
                        "Id",
                        out var idElement)
                        ? idElement.GetInt32()
                        : 0;

                string? groupName = null;

                int? groupId = null;

                if (forum.TryGetProperty(
                        "Group",
                        out var groupElement))
                {
                    if (groupElement.TryGetProperty(
                            "Name",
                            out var groupNameElement))
                    {
                        groupName =
                            groupNameElement.GetString();
                    }

                    if (groupElement.TryGetProperty(
                            "Id",
                            out var groupIdElement))
                    {
                        groupId =
                            groupIdElement.GetInt32();
                    }
                }

                matches.Add(
                    new ForumMatch
                    {
                        ForumId = id,
                        ForumName = name ?? forumName,
                        GroupId = groupId,
                        GroupName = groupName
                    });
            }

            var totalCount =
                document.RootElement.TryGetProperty(
                    "TotalCount",
                    out var totalElement)
                    ? totalElement.GetInt32()
                    : 0;

            var returnedCount =
                forumsElement.GetArrayLength();

            if (returnedCount == 0 ||
                (pageIndex + 1) * pageSize >= totalCount)
            {
                break;
            }

            pageIndex++;
        }

        return matches;
    }

    // internal class ForumMatch
    public class ForumMatch
    {
        public int ForumId { get; set; }

        public string ForumName { get; set; } = "";

        public int? GroupId { get; set; }

        public string? GroupName { get; set; }
    }
}