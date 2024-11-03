// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Api.Server;
using Cratis.Chronicle.Storage;
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
public class WebServer : IStartupTask
{
    readonly WebApplication _webApplication;
    readonly IGrainFactory _grainFactory;
    readonly IStorage _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebServer"/> class.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
    /// <param name="storage">The <see cref="IStorage"/>.</param>
    /// <param name="workbenchOptions">The <see cref="ChronicleWorkbenchOptions"/>.</param>
    public WebServer(
        IGrainFactory grainFactory,
        IStorage storage,
        IOptions<ChronicleWorkbenchOptions> workbenchOptions)
    {
        _grainFactory = grainFactory;
        _storage = storage;

        var builder = WebApplication.CreateBuilder();

        builder.Host
            .UseCratisApplicationModel();

        builder.Services.AddCratisChronicleApi();

        AddServicesFromHost(builder.Services);
        builder.WebHost
            .UseKestrel()
            .UseUrls($"http://*:{workbenchOptions.Value.Port}");

        _webApplication = builder.Build();

        _webApplication.UseCratisChronicleApi();

        var rootType = typeof(WorkbenchApplicationBuilderExtensions);
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
    }

    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken)
    {
        await _webApplication.StartAsync(cancellationToken);
    }

    void AddServicesFromHost(IServiceCollection services)
    {
        services.AddSingleton(_grainFactory);
        services.AddSingleton(_storage);
    }
}
