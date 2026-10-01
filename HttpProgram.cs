// using Microsoft.Extensions.Configuration;
// using ModelContextProtocol.Server;
// using VerintCsharpMcp.Services;

// var builder =
//     WebApplication.CreateBuilder(args);

// builder.Configuration.AddUserSecrets(
//     typeof(HttpProgram).Assembly,
//     optional: true);

// builder.Services.AddHttpClient<VerintClient>(
//     client =>
//     {
//         client.Timeout =
//             TimeSpan.FromSeconds(30);
//     });

// builder.Services.AddSingleton<PendingActionStore>();

// builder.Services
//     .AddMcpServer()
//     .WithHttpTransport(options =>
//     {
//         options.SessionMode =
//             HttpServerSessionMode.Stateless;
//     })
//     .WithToolsFromAssembly();

// var app =
//     builder.Build();

// app.MapMcp("/mcp");

// app.Run("http://127.0.0.1:3000");