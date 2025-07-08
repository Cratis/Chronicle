// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using Cratis.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost.Logging;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a web application factory for Chronicle In Process integration tests.
/// </summary>
/// <typeparam name="TStartup">Type of the startup type.</typeparam>
/// <remarks>When deriving this class and overriding <see cref="ChronicleWebApplicationFactory{TStartup}.ConfigureWebHost"/> remember to call base.ConfigureWebHost.</remarks>
public class ChronicleOrleansInProcessWebApplicationFactory<TStartup>(
    IChronicleSetupFixture fixture,
    Action<IServiceCollection> configureServices,
    ContentRoot contentRoot) : ChronicleWebApplicationFactory<TStartup>(fixture, contentRoot)
    where TStartup : class
{
    readonly IChronicleSetupFixture _fixture = fixture;
    readonly Action<IServiceCollection> _configureServices = configureServices;

    /// <inheritdoc/>
    protected override IHostBuilder CreateHostBuilder()
    {
        var builder = Host.CreateDefaultBuilder();
        var chronicleOptions = new Configuration.ChronicleOptions();

        builder.UseCratisMongoDB(
            mongo =>
            {
                mongo.Server = $"mongodb://localhost:{ChronicleFixture.MongoDBPort}";
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

                _configureServices(services);
            });
        builder.AddCratisChronicle();

        builder.UseOrleans(silo =>
            {
                silo
                    .UseLocalhostClustering()
                    .AddCratisChronicle(
                        options => options.EventStoreName = _fixture.GetEventStoreName(),
                        chronicleBuilder => chronicleBuilder.WithMongoDB(chronicleOptions.Storage.ConnectionDetails, _fixture.GetEventStoreName()));
            })
            .UseConsoleLifetime();

        builder.ConfigureWebHostDefaults(b => b.Configure(app => app.UseCratisChronicle()));
        return builder;
    }
}
