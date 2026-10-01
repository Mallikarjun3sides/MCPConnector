using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using VerintCsharpMcp.Services;

var builder =
    Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets(
    typeof(Program).Assembly,
    optional: true);

builder.Services.AddHttpClient<VerintClient>(
    client =>
    {
        client.Timeout =
            TimeSpan.FromSeconds(30);
    });

builder.Services
    .AddSingleton<PendingActionStore>();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();