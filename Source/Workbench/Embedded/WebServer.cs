// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api;
using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
    IOptions<ChronicleWorkbenchOptions> workbenchOptions) : IHostedService
{
    WebApplication? _webApplication;

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Run(
            async () =>
            {
                var builder = WebApplication.CreateBuilder();
                var chronicleServices = serviceProvider.GetService<IServices>();

                builder.Host
                    .UseCratisApplicationModel(options =>
                    {
                        options.CorrelationId = workbenchOptions.Value.ApplicationModel.CorrelationId;
                        options.Tenancy = workbenchOptions.Value.ApplicationModel.Tenancy;
                        options.IdentityDetailsProvider = workbenchOptions.Value.ApplicationModel.IdentityDetailsProvider;
                    });

                builder.Services.AddCratisChronicleApi(chronicleServices);

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

                await _webApplication.RunAsync();
            },
            cancellationToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await (_webApplication?.DisposeAsync() ?? ValueTask.CompletedTask);
    }
}
