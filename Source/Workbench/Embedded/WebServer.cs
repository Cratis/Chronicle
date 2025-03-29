// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api;
using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Workbench.Embedded;

/// <summary>
/// Represents the WebServer that is hosting the Chronicle Workbench.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="workbenchOptions">The <see cref="ChronicleWorkbenchOptions"/>.</param>
public class WebServer(
    IServiceProvider serviceProvider,
    IOptions<ChronicleWorkbenchOptions> workbenchOptions) : IHostedService, IDisposable
{
    readonly CancellationTokenSource _cancellationTokenSource = new();
    WebApplication? _webApplication;
    bool _disposed;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(
            async () =>
            {
                var builder = WebApplication.CreateBuilder();
                builder.Configuration.Sources.Clear();
                var chronicleServices = serviceProvider.GetService<IServices>();

                builder.Host
                    .UseCratisApplicationModel(options =>
                    {
                        options.CorrelationId = workbenchOptions.Value.ApplicationModel.CorrelationId;
                        options.Tenancy = workbenchOptions.Value.ApplicationModel.Tenancy;
                        options.IdentityDetailsProvider = workbenchOptions.Value.ApplicationModel.IdentityDetailsProvider;
                    });

                builder.Services.AddCratisChronicleApi(chronicleServices);
                builder.Services.Configure<MvcOptions>(options => options.UseRoutePrefix(workbenchOptions.Value.BasePath));
                builder.WebHost
                    .UseKestrel(options =>
                    {
                        options.ConfigureEndpointDefaults(_ => { });
                        options.ListenAnyIP(workbenchOptions.Value.Port);
                    });

                _webApplication = builder.Build();

                var basePath = workbenchOptions.Value.BasePath;
                if (!basePath.StartsWith('/')) basePath = $"/{basePath}";
                if (basePath.EndsWith('/')) basePath = basePath[0..^1];

                _webApplication.UseCratisChronicleApi();

                var rootType = typeof(WorkbenchWebApplicationBuilderExtensions);
                var rootResourceNamespace = $"{rootType.Namespace}.Files";
                IFileProvider fileProvider = new ManifestEmbeddedFileProvider(rootType.Assembly, rootResourceNamespace);

                var indexFile = string.Empty;
                var file = fileProvider.GetFileInfo("index.html");

                if (file.Exists)
                {
                    await using var stream = file.CreateReadStream();
                    using var reader = new StreamReader(stream);
                    indexFile = await reader.ReadToEndAsync();

                    indexFile = indexFile
                        .Replace("src=\"/", $"src=\"{basePath}/")
                        .Replace("href=\"/", $"href=\"{basePath}/")
                        .Replace("name=\"base-path\" content=\"\"", $"name=\"base-path\" content=\"{basePath}\"");
                }
                fileProvider = new StaticFilesFileProvider(fileProvider, basePath, indexFile);

                _webApplication.UseDefaultFiles(new DefaultFilesOptions
                {
                    FileProvider = fileProvider,
                    RequestPath = basePath
                });
                var staticFileOptions = new StaticFileOptions
                {
                    FileProvider = fileProvider,
                    RequestPath = string.Empty
                };
                _webApplication.UseStaticFiles(staticFileOptions);
                _webApplication.MapFallbackToFile("index.html", staticFileOptions);

                await _webApplication.RunAsync(_cancellationTokenSource.Token);
            });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cancellationTokenSource.CancelAsync();
        await (_webApplication?.DisposeAsync() ?? ValueTask.CompletedTask);
        _cancellationTokenSource.Dispose();
        _disposed = true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
