// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;

using System.Net;
using System.Net.Sockets;
using Cratis.Arc;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.AspNetCore.Identities;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using Cratis.Json;
using Cratis.Serialization;
using KernelCore::Cratis.Chronicle.Diagnostics.OpenTelemetry;
using KernelCore::Cratis.Chronicle.Observation.Reactors.Clients;
using KernelCore::Cratis.Chronicle.Observation.Reducers.Clients;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    readonly IChronicleSetupFixture _fixture = fixture;

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

        var siloPort = GetFreePort();
        var gatewayPort = GetFreePort();

        var delegatingProvider = new DelegatingClientArtifactsProvider(_fixture);
        DelegatingClientArtifactsProvider.Instance = delegatingProvider;

        builder.UseOrleans(silo =>
            {
                silo.UseLocalhostClustering(siloPort, gatewayPort);

                ConceptTypeConvertersRegistrar.EnsureFor(typeof(ChronicleOrleansInProcessWebApplicationFactory<TStartup>).Assembly);
                ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();

                KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                    silo,
                    chronicleBuilder => chronicleBuilder.WithMongoDB(mongoServer, Constants.EventStore));

                silo.AddActivityPropagation();

                silo.ConfigureServices(services =>
                {
                    services.AddTypeDiscovery();
                    services.AddBindingsByConvention();
                    services.AddSelfBindings();

                    services.AddSingleton<IReactorMediator, ReactorMediator>();
                    services.AddSingleton<IReducerMediator, ReducerMediator>();
                    services.AddSingleton<INamingPolicy>(new DefaultNamingPolicy());
                    services.AddSingleton<IIdentityProvider>(sp => new IdentityProvider(
                        sp.GetRequiredService<IHttpContextAccessor>(),
                        sp.GetRequiredService<ILogger<IdentityProvider>>()));
                    services.AddSingleton(Globals.JsonSerializerOptions);
                    services.AddHttpContextAccessor();

                    // Ensure DelegatingClientArtifactsProvider wins over convention-based
                    // discovery from AddBindingsByConvention above, which registers test
                    // fixture types as IClientArtifactsProvider.
                    services.RemoveAll<IClientArtifactsProvider>();
                    services.AddSingleton<IClientArtifactsProvider>(delegatingProvider);

                    services.AddSingleton<IChronicleClient>(sp =>
                    {
                        var options = sp.GetRequiredService<IOptions<ChronicleOptions>>().Value;
                        var artifactsProvider = sp.GetRequiredService<IClientArtifactsProvider>();
                        var identityProvider = new IdentityProvider(
                            sp.GetRequiredService<IHttpContextAccessor>(),
                            sp.GetRequiredService<ILogger<IdentityProvider>>());

                        var grainFactory = sp.GetRequiredService<IGrainFactory>();
                        var chronicleServices = sp.GetRequiredService<IServices>();

                        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

                        var connectionLifecycle = new ConnectionLifecycle(loggerFactory.CreateLogger<ConnectionLifecycle>());
                        var connection = new ChronicleConnection(connectionLifecycle, grainFactory, loggerFactory);
                        connection.SetServices(chronicleServices);
                        return new ChronicleClient(connection, options, artifactsProvider, sp, identityProvider, loggerFactory: loggerFactory);
                    });

                    services.AddSingleton(sp =>
                    {
                        var client = sp.GetRequiredService<IChronicleClient>();
                        return client.GetEventStore(Constants.EventStore).GetAwaiter().GetResult();
                    });

                    services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Connection);
                    services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().UnitOfWorkManager);
                    services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().EventTypes);
                    services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().EventLog);
                    services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Reactors);
                    services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Reducers);
                    services.AddSingleton(sp => sp.GetRequiredService<IEventStore>().Projections);
                });
            })
            .UseConsoleLifetime();

        builder.ConfigureWebHostDefaults(b =>
        {
            b.Configure(app => app.UseCratisChronicle());
            configureWebHost(b);
        });

        return builder;
    }

    static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
