// using ModelContextProtocol.Server;
// using System.ComponentModel;
// using VerintCsharpMcp.Services;

// namespace VerintCsharpMcp.Tools;

// [McpServerToolType]
// public class SearchTool
// {
//     [McpServerTool]
//     [Description(
//         "Search Verint Community for users, posts, forums, blogs, and other community content.")]
//     public static async Task<string> SearchVerint(
//         VerintClient verintClient,
//         [Description("The search text")] string query)
//     {
//         if (string.IsNullOrWhiteSpace(query))
//         {
//             return "Search query cannot be empty.";
//         }

//         var encodedQuery =
//             Uri.EscapeDataString(query);

//         var url =
//             $"api.ashx/v2/search.json?q={encodedQuery}";

//         return await verintClient.GetAsync(url);
//     }
// }




using ModelContextProtocol.Server;
using System.ComponentModel;
using VerintCsharpMcp.Services;

namespace VerintCsharpMcp.Tools;

[McpServerToolType]
public class SearchTool
{
    [McpServerTool]
    [Description(
        "Search Verint Community for users, posts, forums, blogs, and other community content.")]
    public static async Task<string> SearchVerint(
        VerintClient verintClient,
        [Description("The search text")] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Search query cannot be empty.";
        }

        var encodedQuery =
            Uri.EscapeDataString(query);

        var url =
            $"api.ashx/v2/search.json?query={encodedQuery}";

        try
        {
            return await verintClient.GetAsync(url);
        }
        catch (Exception ex)
        {
            return $"SEARCH ERROR: {ex.Message}";
        }
    }
}