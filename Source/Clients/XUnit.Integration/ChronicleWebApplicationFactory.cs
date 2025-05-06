// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using Cratis.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost.Logging;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a web application factory for Chronicle integration tests.
/// </summary>
/// <param name="artifactsProvider">The client artifacts provider.</param>
/// <param name="configureServices">Action to configure the services.</param>
/// <param name="contentRoot">The content root path.</param>
/// <typeparam name="TStartup">Type of the startup type.</typeparam>
public class ChronicleWebApplicationFactory<TStartup>(
    IClientArtifactsProvider artifactsProvider,
    Action<IServiceCollection> configureServices,
    string contentRoot) : WebApplicationFactory<TStartup>
    where TStartup : class
{
    /// <inheritdoc/>
    protected override IHostBuilder CreateHostBuilder()
    {
        var builder = Host.CreateDefaultBuilder();
        var chronicleOptions = new Concepts.Configuration.ChronicleOptions();

        builder.UseCratisMongoDB(
            mongo =>
            {
                mongo.Server = "mongodb://localhost:27018";
                mongo.Database = "orleans";
            });
        builder.ConfigureLogging(_ =>
        {
            _.ClearProviders();
            _.AddFile($"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.log");
        });
        builder
            .UseDefaultServiceProvider(_ => _.ValidateOnBuild = false)
            .ConfigureServices((ctx, services) =>
            {
                services.AddCratisApplicationModelMeter();
                services.AddSingleton(Globals.JsonSerializerOptions);
                services.AddBindingsByConvention();
                services.AddSelfBindings();
                services.AddChronicleTelemetry(ctx.Configuration);
                services.AddControllers();
                ctx.Configuration.Bind(chronicleOptions);
                services.Configure<ChronicleOptions>(opts => opts.ArtifactsProvider = artifactsProvider);

                configureServices(services);
            });
        builder.AddCratisChronicle();

        builder.UseOrleans(silo =>
            {
                silo
                    .UseLocalhostClustering()
                    .AddCratisChronicle(
                        options => options.EventStoreName = Constants.EventStore,
                        chronicleBuilder => chronicleBuilder.WithMongoDB(chronicleOptions.Storage.ConnectionDetails, Constants.EventStore));
            })
            .UseConsoleLifetime();

        builder.ConfigureWebHostDefaults(b => b.Configure(app => app.UseCratisChronicle()));
        return builder;
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(contentRoot);
    }
}
