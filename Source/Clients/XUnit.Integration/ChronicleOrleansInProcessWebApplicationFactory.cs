// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using Cratis.Arc;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.InProcess;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using KernelCore::Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost.Logging;
using Configuration = KernelCore::Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a web application factory for Chronicle In Process integration tests.
/// </summary>
/// <param name="fixture">The <see cref="IChronicleSetupFixture"/>.</param>
/// <param name="configureServices">Action to configure the services.</param>
/// <param name="configureMongoDB">Action to configure MongoDB options.</param>
/// <param name="configureWebHost">Action to configure <see cref="IWebHostBuilder"/>.</param>
/// <param name="contentRoot">The content root path.</param>
/// <typeparam name="TStartup">Type of the startup type.</typeparam>
/// <remarks>When deriving this class and overriding <see cref="ChronicleWebApplicationFactory{TStartup}.ConfigureWebHost"/> remember to call base.ConfigureWebHost.</remarks>
public class ChronicleOrleansInProcessWebApplicationFactory<TStartup>(
    IChronicleSetupFixture fixture,
    Action<IServiceCollection> configureServices,
    Action<IMongoDBBuilder> configureMongoDB,
    Action<IWebHostBuilder> configureWebHost,
    ContentRoot contentRoot) : ChronicleWebApplicationFactory<TStartup>(fixture, contentRoot)
    where TStartup : class
{
    /// <inheritdoc/>
    protected override IHostBuilder CreateHostBuilder()
    {
        var builder = Host.CreateDefaultBuilder();
        var chronicleOptions = new Configuration.ChronicleOptions();

        var mongoServer = $"mongodb://localhost:{ChronicleFixture.MongoDBPort}/?directConnection=true";

        builder.AddCratisMongoDB(
            mongo =>
            {
                mongo.Server = mongoServer;
                mongo.Database = "orleans";
                mongo.DirectConnection = true;
            },
            configureMongoDB);
        builder.ConfigureLogging(_ =>
        {
            _.ClearProviders();
            _.AddFile($"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.log");
        });
        builder
            .UseDefaultServiceProvider(_ => _.ValidateOnBuild = false)
            .ConfigureServices((ctx, services) =>
            {
                services.AddCratisArcMeter();
                services.AddBindingsByConvention();
                services.AddSelfBindings();
                services.AddChronicleTelemetry(ctx.Configuration);
                services.AddControllers();
                ctx.Configuration.Bind(chronicleOptions);

                configureServices(services);
            });
        builder.AddCratisChronicle();

        builder.UseOrleans(silo =>
            {
                silo
                    .UseLocalhostClustering()
                    .AddCratisChronicle(
                        options => options.EventStoreName = Constants.EventStore,
                        chronicleBuilder => chronicleBuilder.WithMongoDB(mongoServer, Constants.EventStore));
            })
            .UseConsoleLifetime();

        builder.ConfigureWebHostDefaults(b =>
        {
            b.Configure(app => app.UseCratisChronicle());
            configureWebHost(b);
        });
        return builder;
    }
}
