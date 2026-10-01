using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace VerintCsharpMcp.Services;

public class VerintClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public VerintClient(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    private string BaseUrl =>
        _configuration["Verint:BaseUrl"]
        ?? throw new Exception(
            "Verint:BaseUrl is not configured.");

    private string AccessToken =>
        _configuration["Verint:AccessToken"]
        ?? throw new Exception(
            "Verint:AccessToken is not configured.");

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string relativeUrl)
    {
        var baseUrl = BaseUrl.TrimEnd('/');

        var request = new HttpRequestMessage(
            method,
            $"{baseUrl}/{relativeUrl.TrimStart('/')}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                AccessToken);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        return request;
    }

    // ============================================================
    // GET
    // ============================================================

    public async Task<string> GetAsync(
        string relativeUrl)
    {
        using var request =
            CreateRequest(
                HttpMethod.Get,
                relativeUrl);

        using var response =
            await _httpClient.SendAsync(request);

        var content =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"GET {relativeUrl} failed. " +
                $"HTTP {(int)response.StatusCode} " +
                $"{response.StatusCode}\n" +
                content);
        }

        return content;
    }

    // ============================================================
    // POST
    // ============================================================

    public async Task<string> PostAsync(
        string relativeUrl,
        string json)
    {
        using var request =
            CreateRequest(
                HttpMethod.Post,
                relativeUrl);

        request.Content =
            new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

        using var response =
            await _httpClient.SendAsync(request);

        var content =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"POST {relativeUrl} failed. " +
                $"HTTP {(int)response.StatusCode} " +
                $"{response.StatusCode}\n" +
                content);
        }

        return content;
    }

    // ============================================================
    // PUT
    // Verint REST API:
    // POST + Rest-Method: PUT
    // ============================================================

    public async Task<string> PutAsync(
        string relativeUrl,
        string json)
    {
        using var request =
            CreateRequest(
                HttpMethod.Post,
                relativeUrl);

        request.Headers.Add(
            "Rest-Method",
            "PUT");

        request.Content =
            new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

        using var response =
            await _httpClient.SendAsync(request);

        var content =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"POST {relativeUrl} " +
                $"(Rest-Method: PUT) failed. " +
                $"HTTP {(int)response.StatusCode} " +
                $"{response.StatusCode}\n" +
                content);
        }

        return content;
    }

    // ============================================================
    // DELETE
    // Verint REST API:
    // POST + Rest-Method: DELETE
    // ============================================================

    public async Task<string> DeleteAsync(
        string relativeUrl,
        string? json = null)
    {
        using var request =
            CreateRequest(
                HttpMethod.Post,
                relativeUrl);

        request.Headers.Add(
            "Rest-Method",
            "DELETE");

        if (!string.IsNullOrWhiteSpace(json))
        {
            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");
        }

        using var response =
            await _httpClient.SendAsync(request);

        var content =
            await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"POST {relativeUrl} " +
                $"(Rest-Method: DELETE) failed. " +
                $"HTTP {(int)response.StatusCode} " +
                $"{response.StatusCode}\n" +
                content);
        }

        return content;
    }
}




// using System.Net.Http.Headers;
// using System.Text;
// using Microsoft.Extensions.Configuration;

// namespace VerintCsharpMcp.Services;

// public class VerintClient
// {
//     private readonly HttpClient _httpClient;
//     private readonly IConfiguration _configuration;

//     public VerintClient(
//         HttpClient httpClient,
//         IConfiguration configuration)
//     {
//         _httpClient = httpClient;
//         _configuration = configuration;
//     }

//     private string BaseUrl =>
//         _configuration["Verint:BaseUrl"]
//         ?? throw new Exception(
//             "Verint:BaseUrl is not configured.");

//     private string AccessToken =>
//         _configuration["Verint:AccessToken"]
//         ?? throw new Exception(
//             "Verint:AccessToken is not configured.");

//     private HttpRequestMessage CreateRequest(
//         HttpMethod method,
//         string relativeUrl)
//     {
//         var baseUrl = BaseUrl.TrimEnd('/');

//         var request = new HttpRequestMessage(
//             method,
//             $"{baseUrl}/{relativeUrl.TrimStart('/')}");

//         request.Headers.Authorization =
//             new AuthenticationHeaderValue(
//                 "Bearer",
//                 AccessToken);

//         request.Headers.Accept.Add(
//             new MediaTypeWithQualityHeaderValue(
//                 "application/json"));

//         return request;
//     }

//     public async Task<string> GetAsync(string relativeUrl)
//     {
//         using var request =
//             CreateRequest(HttpMethod.Get, relativeUrl);

//         using var response =
//             await _httpClient.SendAsync(request);

//         var content =
//             await response.Content.ReadAsStringAsync();

//         if (!response.IsSuccessStatusCode)
//         {
//             throw new Exception(
//                 $"GET {relativeUrl} failed. " +
//                 $"HTTP {(int)response.StatusCode} " +
//                 $"{response.StatusCode}\n" +
//                 content);
//         }

//         return content;
//     }

//     public async Task<string> PostAsync(
//         string relativeUrl,
//         string json)
//     {
//         using var request =
//             CreateRequest(HttpMethod.Post, relativeUrl);

//         request.Content =
//             new StringContent(
//                 json,
//                 Encoding.UTF8,
//                 "application/json");

//         using var response =
//             await _httpClient.SendAsync(request);

//         var content =
//             await response.Content.ReadAsStringAsync();

//         if (!response.IsSuccessStatusCode)
//         {
//             throw new Exception(
//                 $"POST {relativeUrl} failed. " +
//                 $"HTTP {(int)response.StatusCode} " +
//                 $"{response.StatusCode}\n" +
//                 content);
//         }

//         return content;
//     }

//     public async Task<string> PutAsync(
//         string relativeUrl,
//         string json)
//     {
//         using var request =
//             CreateRequest(HttpMethod.Put, relativeUrl);

//         // request.Headers.Add("Rest-Method", "PUT");

//         request.Content =
//             new StringContent(
//                 json,
//                 Encoding.UTF8,
//                 "application/json");

//         using var response =
//             await _httpClient.SendAsync(request);

//         var content =
//             await response.Content.ReadAsStringAsync();

//         if (!response.IsSuccessStatusCode)
//         {
//             throw new Exception(
//                 $"PUT {relativeUrl} failed. " +
//                 $"HTTP {(int)response.StatusCode} " +
//                 $"{response.StatusCode}\n" +
//                 content);
//         }

//         return content;
//     }

//     // public async Task<string> DeleteAsync(
//     //     string relativeUrl)
//     // {
//     //     using var request =
//     //         CreateRequest(HttpMethod.Delete, relativeUrl);

//     //     using var response =
//     //         await _httpClient.SendAsync(request);

//     //     var content =
//     //         await response.Content.ReadAsStringAsync();

//     //     if (!response.IsSuccessStatusCode)
//     //     {
//     //         throw new Exception(
//     //             $"DELETE {relativeUrl} failed. " +
//     //             $"HTTP {(int)response.StatusCode} " +
//     //             $"{response.StatusCode}\n" +
//     //             content);
//     //     }

//     //     return content;
//     // }

//     public async Task<string> DeleteAsync(string relativeUrl)
//     {
//         var request = CreateRequest(
//             HttpMethod.Post,
//             relativeUrl);

//         // request.Headers.Add("Rest-Method", "DELETE");

//         using var response =
//             await _httpClient.SendAsync(request);

//         var content =
//             await response.Content.ReadAsStringAsync();

//         if (!response.IsSuccessStatusCode)
//         {
//             throw new Exception(
//                 $"DELETE {relativeUrl} failed. " +
//                 $"HTTP {(int)response.StatusCode} " +
//                 $"{response.StatusCode}\n{content}");
//         }

//         return content;
//     }
// }