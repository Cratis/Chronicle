// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Workbench.Embedded;

/// <summary>
/// Represents the WebServer that is hosting the Chronicle Workbench.
/// </summary>
/// <param name="workbenchOptions">The <see cref="ChronicleWorkbenchOptions"/>.</param>
public class WebServer(IOptions<ChronicleWorkbenchOptions> workbenchOptions) : IHostedService
{
    WebApplication? _webApplication;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Host
            .UseCratisApplicationModel();

        builder.Services.AddCratisChronicleApi();

        builder.WebHost
            .UseKestrel()
            .UseUrls($"http://*:{workbenchOptions.Value.Port}");

        _webApplication = builder.Build();

        _webApplication.UseCratisChronicleApi();

        var rootType = typeof(WorkbenchWebApplicationBuilderExtensions);
        var rootResourceNamespace = $"{rootType.Namespace}.Files";
        var fileProvider = new ManifestEmbeddedFileProvider(rootType.Assembly, rootResourceNamespace);
        _webApplication.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = fileProvider
        });
        var staticFileOptions = new StaticFileOptions
        {
            FileProvider = fileProvider,
            RequestPath = string.Empty
        };
        _webApplication.UseStaticFiles(staticFileOptions);
        _webApplication.MapFallbackToFile("index.html", staticFileOptions);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await (_webApplication?.DisposeAsync() ?? ValueTask.CompletedTask);
    }
}
