using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using VerintCsharpMcp.Models;
using VerintCsharpMcp.Services;

namespace VerintCsharpMcp.Tools;

[McpServerToolType]
public class BlogTool
{
    [McpServerTool]
    [Description(
        "Draft a NEW blog post in Verint Community. " +
        "Use this tool only when the user explicitly wants to publish/create a post in a BLOG. " +
        "The blog post is not created until confirmation."
    )]
    public static async Task<string> DraftCreateBlogPost(
        PendingActionStore store,
        VerintClient verintClient,

        [Description("Blog name where the post must be created.")]
        string blogName,

        [Description("Title of the new blog post.")]
        string title,

        [Description("Body/content of the new blog post.")]
        string body,

        [Description("Optional blog ID.")]
        int? blogId = null)
    {
        if (string.IsNullOrWhiteSpace(blogName))
            return "Blog name is required.";

        if (string.IsNullOrWhiteSpace(title))
            return "Blog post title is required.";

        if (string.IsNullOrWhiteSpace(body))
            return "Blog post body is required.";

        var blogResult = await ResolveBlog(
            verintClient,
            blogName.Trim(),
            blogId);

        if (!blogResult.Success)
            return blogResult.Message;

        var action = new PendingAction
        {
            ActionType = ActionType.Create,

            // IMPORTANT
            Target = ActionTarget.Blog,

            BlogId = blogResult.Blog!.BlogId,

            Title = title.Trim(),
            Body = body
        };

        store.Add(action);

        return $"""
        BLOG POST DRAFT CREATED

        Action ID: {action.ActionId}

        Target: Blog
        Blog: {blogResult.Blog.BlogName}
        Blog ID: {blogResult.Blog.BlogId}

        Title:
        {action.Title}

        Body:
        {action.Body}

        The blog post has NOT been created yet.

        Explicit confirmation is required.

        Action ID:
        {action.ActionId}
        """;
    }


    [McpServerTool]
    [Description(
        "Draft an EDIT to an existing Verint Community blog post. " +
        "Use only for blog posts."
    )]
    public static async Task<string> DraftEditBlogPost(
        PendingActionStore store,
        VerintClient verintClient,

        [Description("Blog name.")]
        string blogName,

        [Description("Current blog post title/name.")]
        string postName,

        [Description("New blog post title.")]
        string title,

        [Description("New blog post body.")]
        string body,

        [Description("Optional blog ID.")]
        int? blogId = null,

        [Description("Optional blog post ID.")]
        int? postId = null)
    {
        if (string.IsNullOrWhiteSpace(blogName))
            return "Blog name is required.";

        if (string.IsNullOrWhiteSpace(postName))
            return "Blog post name is required.";

        if (string.IsNullOrWhiteSpace(title))
            return "New blog post title is required.";

        if (string.IsNullOrWhiteSpace(body))
            return "New blog post body is required.";

        var blogResult = await ResolveBlog(
            verintClient,
            blogName.Trim(),
            blogId);

        if (!blogResult.Success)
            return blogResult.Message;

        var postResult = await ResolvePost(
            verintClient,
            blogResult.Blog!.BlogId,
            postName.Trim(),
            postId);

        if (!postResult.Success)
            return postResult.Message;

        var action = new PendingAction
        {
            ActionType = ActionType.Edit,

            Target = ActionTarget.Blog,

            BlogId = blogResult.Blog.BlogId,
            BlogPostId = postResult.Post!.BlogPostId,

            Title = title.Trim(),
            Body = body
        };

        store.Add(action);

        return $"""
        BLOG POST EDIT DRAFT CREATED

        Action ID: {action.ActionId}

        Target: Blog
        Blog: {blogResult.Blog.BlogName}
        Blog ID: {blogResult.Blog.BlogId}

        Post:
        {postResult.Post.Title}

        Post ID:
        {postResult.Post.BlogPostId}

        New Title:
        {action.Title}

        New Body:
        {action.Body}

        The blog post has NOT been changed.

        Explicit confirmation is required.

        Action ID:
        {action.ActionId}
        """;
    }


    [McpServerTool]
    [Description(
        "Draft deletion of an existing Verint Community blog post. " +
        "Use only for blog posts."
    )]
    public static async Task<string> DraftDeleteBlogPost(
        PendingActionStore store,
        VerintClient verintClient,

        [Description("Blog name.")]
        string blogName,

        [Description("Blog post title/name.")]
        string postName,

        [Description("Optional blog ID.")]
        int? blogId = null,

        [Description("Optional blog post ID.")]
        int? postId = null)
    {
        if (string.IsNullOrWhiteSpace(blogName))
            return "Blog name is required.";

        if (string.IsNullOrWhiteSpace(postName))
            return "Blog post name is required.";

        var blogResult = await ResolveBlog(
            verintClient,
            blogName.Trim(),
            blogId);

        if (!blogResult.Success)
            return blogResult.Message;

        var postResult = await ResolvePost(
            verintClient,
            blogResult.Blog!.BlogId,
            postName.Trim(),
            postId);

        if (!postResult.Success)
            return postResult.Message;

        var action = new PendingAction
        {
            ActionType = ActionType.Delete,

            Target = ActionTarget.Blog,

            BlogId = blogResult.Blog.BlogId,
            BlogPostId = postResult.Post!.BlogPostId,

            Title = postResult.Post.Title
        };

        store.Add(action);

        return $"""
        BLOG POST DELETE DRAFT CREATED

        Action ID: {action.ActionId}

        Target: Blog
        Blog: {blogResult.Blog.BlogName}
        Blog ID: {blogResult.Blog.BlogId}

        Post:
        {postResult.Post.Title}

        Post ID:
        {postResult.Post.BlogPostId}

        The blog post has NOT been deleted.

        Explicit confirmation is required.

        Action ID:
        {action.ActionId}
        """;
    }


    private static async Task<BlogResolution> ResolveBlog(
        VerintClient verintClient,
        string blogName,
        int? blogId)
    {
        var blogs = await GetAllBlogs(verintClient);

        if (blogs.Count == 0)
        {
            return BlogResolution.Fail(
                $"NO BLOGS FOUND\n\nBlog: {blogName}");
        }

        var matches = blogs
            .Where(x =>
                string.Equals(
                    x.BlogName,
                    blogName,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (blogId.HasValue)
        {
            var selected = matches.FirstOrDefault(x =>
                x.BlogId == blogId.Value);

            if (selected == null)
            {
                return BlogResolution.Fail(
                    $"BLOG ID DOES NOT MATCH BLOG NAME\n\n" +
                    $"Blog: {blogName}\n" +
                    $"Blog ID: {blogId.Value}");
            }

            return BlogResolution.SuccessResult(selected);
        }

        if (matches.Count == 0)
        {
            return BlogResolution.Fail(
                $"NO BLOG FOUND\n\nBlog: {blogName}");
        }

        if (matches.Count > 1)
        {
            var response =
                $"MULTIPLE BLOGS FOUND\n\n" +
                $"Blog name: {blogName}\n\n";

            foreach (var blog in matches)
            {
                response +=
                    $"Blog ID: {blog.BlogId}\n" +
                    $"Blog: {blog.BlogName}\n" +
                    $"Group: {blog.GroupName ?? "N/A"}\n\n";
            }

            response += "Please provide the Blog ID.";

            return BlogResolution.Fail(response);
        }

        return BlogResolution.SuccessResult(matches[0]);
    }


    private static async Task<List<BlogMatch>> GetAllBlogs(
        VerintClient verintClient)
    {
        var result = new List<BlogMatch>();

        int pageIndex = 0;
        const int pageSize = 100;

        while (true)
        {
            var url =
                $"api.ashx/v2/blogs.json" +
                $"?PageIndex={pageIndex}" +
                $"&PageSize={pageSize}";

            var json = await verintClient.GetAsync(url);

            using var document =
                JsonDocument.Parse(json);

            var root = document.RootElement;

            if (!root.TryGetProperty(
                    "Blogs",
                    out var blogsElement))
                break;

            var returnedCount = 0;

            foreach (var blog in blogsElement.EnumerateArray())
            {
                returnedCount++;

                if (!blog.TryGetProperty(
                        "Id",
                        out var idElement))
                    continue;

                if (!blog.TryGetProperty(
                        "Name",
                        out var nameElement))
                    continue;

                var match = new BlogMatch
                {
                    BlogId = idElement.GetInt32(),
                    BlogName = nameElement.GetString() ?? string.Empty
                };

                if (blog.TryGetProperty(
                        "Group",
                        out var groupElement))
                {
                    if (groupElement.TryGetProperty(
                            "Id",
                            out var groupId))
                    {
                        match.GroupId = groupId.GetInt32();
                    }

                    if (groupElement.TryGetProperty(
                            "Name",
                            out var groupName))
                    {
                        match.GroupName =
                            groupName.GetString();
                    }
                }

                result.Add(match);
            }

            if (returnedCount < pageSize)
                break;

            pageIndex++;
        }

        return result;
    }


    private static async Task<PostResolution> ResolvePost(
        VerintClient verintClient,
        int blogId,
        string postName,
        int? postId)
    {
        var posts =
            await GetAllPosts(
                verintClient,
                blogId);

        if (postId.HasValue)
        {
            var selected = posts.FirstOrDefault(
                x => x.BlogPostId == postId.Value);

            if (selected == null)
            {
                return PostResolution.Fail(
                    $"BLOG POST ID NOT FOUND\n\n" +
                    $"Blog ID: {blogId}\n" +
                    $"Post ID: {postId.Value}");
            }

            if (!string.Equals(
                    selected.Title,
                    postName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return PostResolution.Fail(
                    $"BLOG POST NAME DOES NOT MATCH POST ID\n\n" +
                    $"Expected: {postName}\n" +
                    $"Actual: {selected.Title}\n" +
                    $"Post ID: {postId.Value}");
            }

            return PostResolution.SuccessResult(selected);
        }

        var matches = posts
            .Where(x =>
                string.Equals(
                    x.Title,
                    postName,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return PostResolution.Fail(
                $"NO BLOG POST FOUND\n\n" +
                $"Blog ID: {blogId}\n" +
                $"Post: {postName}");
        }

        if (matches.Count > 1)
        {
            var response =
                $"MULTIPLE BLOG POSTS FOUND\n\n" +
                $"Post: {postName}\n\n";

            foreach (var post in matches)
            {
                response +=
                    $"Post ID: {post.BlogPostId}\n" +
                    $"Title: {post.Title}\n\n";
            }

            response +=
                "Please provide the Blog Post ID.";

            return PostResolution.Fail(response);
        }

        return PostResolution.SuccessResult(matches[0]);
    }


    private static async Task<List<BlogPostMatch>> GetAllPosts(
        VerintClient verintClient,
        int blogId)
    {
        var result = new List<BlogPostMatch>();

        int pageIndex = 0;
        const int pageSize = 100;

        while (true)
        {
            var url =
                $"api.ashx/v2/blogs/{blogId}/posts.json" +
                $"?PageIndex={pageIndex}" +
                $"&PageSize={pageSize}";

            var json =
                await verintClient.GetAsync(url);

            using var document =
                JsonDocument.Parse(json);

            var root = document.RootElement;

            if (!root.TryGetProperty(
                    "BlogPosts",
                    out var postsElement))
                break;

            var returnedCount = 0;

            foreach (var post in postsElement.EnumerateArray())
            {
                returnedCount++;

                if (!post.TryGetProperty(
                        "Id",
                        out var idElement))
                    continue;

                if (!post.TryGetProperty(
                        "Title",
                        out var titleElement))
                    continue;

                result.Add(new BlogPostMatch
                {
                    BlogPostId = idElement.GetInt32(),
                    Title = titleElement.GetString() ?? string.Empty
                });
            }

            if (returnedCount < pageSize)
                break;

            pageIndex++;
        }

        return result;
    }


    public class BlogMatch
    {
        public int BlogId { get; set; }

        public string BlogName { get; set; } = string.Empty;

        public int? GroupId { get; set; }

        public string? GroupName { get; set; }
    }


    private class BlogPostMatch
    {
        public int BlogPostId { get; set; }

        public string Title { get; set; } = string.Empty;
    }


    private class BlogResolution
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public BlogMatch? Blog { get; set; }

        public static BlogResolution SuccessResult(
            BlogMatch blog)
        {
            return new BlogResolution
            {
                Success = true,
                Blog = blog
            };
        }

        public static BlogResolution Fail(
            string message)
        {
            return new BlogResolution
            {
                Success = false,
                Message = message
            };
        }
    }


    private class PostResolution
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public BlogPostMatch? Post { get; set; }

        public static PostResolution SuccessResult(
            BlogPostMatch post)
        {
            return new PostResolution
            {
                Success = true,
                Post = post
            };
        }

        public static PostResolution Fail(
            string message)
        {
            return new PostResolution
            {
                Success = false,
                Message = message
            };
        }
    }
}