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
/// <param name="configureStorage">Optional action to configure Chronicle storage on the in-process silo instead of the default MongoDB.</param>
/// <param name="defaultSinkTypeId">Optional default sink type identifier for projection registration.</param>
/// <param name="contentRoot">The content root path.</param>
/// <typeparam name="TStartup">Type of the startup type.</typeparam>
/// <remarks>When deriving this class and overriding <see cref="ChronicleWebApplicationFactory{TStartup}.ConfigureWebHost"/> remember to call base.ConfigureWebHost.</remarks>
public class ChronicleOrleansInProcessWebApplicationFactory<TStartup>(
    IChronicleSetupFixture fixture,
    Action<IServiceCollection> configureServices,
    Action<IMongoDBBuilder> configureMongoDB,
    Action<IWebHostBuilder> configureWebHost,
    Action<KernelCore::Cratis.Chronicle.Configuration.IChronicleBuilder>? configureStorage,
    Cratis.Chronicle.Sinks.SinkTypeId? defaultSinkTypeId,
    ContentRoot contentRoot) : ChronicleWebApplicationFactory<TStartup>(fixture, contentRoot)
    where TStartup : class
{
    readonly IChronicleSetupFixture _fixture = fixture;

    /// <inheritdoc/>
    protected override IHostBuilder CreateHostBuilder()
    {
        var builder = Host.CreateDefaultBuilder();
        var chronicleOptions = new Configuration.ChronicleOptions();

        var mongoServer = $"mongodb://localhost:{_fixture.MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true";

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
            .UseServiceProviderFactory(new FallbackServiceProviderFactory())
            .ConfigureServices((ctx, services) =>
            {
                services.AddCratisArcMeter();
                services.AddBindingsByConvention();
                services.AddSelfBindings();
                services.AddChronicleTelemetry(ctx.Configuration);
                services.AddControllers();
                ctx.Configuration.Bind(chronicleOptions);

                // Keep every host-level Chronicle client registration pointed at the
                // shared test event store so reconnect does not create a second unnamed
                // event store with its own failing OnConnected registration.
                services.PostConfigure<Cratis.Chronicle.ChronicleClientOptions>(options => options.EventStore = Constants.EventStore);
                services.PostConfigure<Microsoft.AspNetCore.Builder.ChronicleAspNetCoreOptions>(options => options.EventStore = Constants.EventStore);
                if (defaultSinkTypeId is not null)
                {
                    services.PostConfigure<Cratis.Chronicle.ChronicleClientOptions>(options => options.DefaultSinkTypeId = defaultSinkTypeId);
                }

                // Register test services directly in DI so the first test works normally,
                // and also capture them in the MutableServiceRegistry so subsequent tests
                // can update instances without rebuilding the container.
                var capturingCollection = new CapturingServiceCollection();
                configureServices(capturingCollection);
                var testServiceRegistry = new MutableServiceRegistry();
                testServiceRegistry.Update(capturingCollection);
                services.AddSingleton(testServiceRegistry);

                // Register delegate factories that always resolve from MutableServiceRegistry.
                // AddTransient ensures the factory runs on every resolution, so that when
                // the registry is updated between tests, subsequent resolutions get the new
                // per-test instance rather than a stale cached singleton.
                foreach (var descriptor in capturingCollection)
                {
                    services.AddTransient(descriptor.ServiceType, sp =>
                    {
                        var registry = sp.GetRequiredService<MutableServiceRegistry>();
                        return registry.TryGet(descriptor.ServiceType, sp)
                            ?? ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType ?? descriptor.ServiceType);
                    });
                }

                // Replace the convention-registered ClientArtifactsActivator with one
                // that wraps any IServiceProvider with a FallbackServiceProvider before
                // delegating. Microsoft DI's internal IServiceProvider injection bypasses
                // our FallbackServiceProviderFactory wrapper, so this is the only
                // reliable way to make MutableServiceRegistry types resolvable
                // during artifact activation. We use RemoveAll + Add to ensure our
                // registration wins regardless of ordering with AddBindingsByConvention.
                services.RemoveAll<IClientArtifactsActivator>();
                services.AddSingleton<IClientArtifactsActivator>(sp =>
                {
                    var registry = sp.GetRequiredService<MutableServiceRegistry>();
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    return new TestClientArtifactsActivator(sp, registry, loggerFactory);
                });
            });
        builder.AddCratisChronicle();

        var siloPort = GetFreePort();
        var gatewayPort = GetFreePort();
        var clusterId = Guid.NewGuid().ToString("N");

        var delegatingProvider = DelegatingClientArtifactsProvider.GetOrCreate(_fixture);

        builder.UseOrleans(silo =>
            {
                silo.UseLocalhostClustering(siloPort, gatewayPort, serviceId: clusterId, clusterId: clusterId);

                ConceptTypeConvertersRegistrar.EnsureFor(typeof(ChronicleOrleansInProcessWebApplicationFactory<TStartup>).Assembly);
                ConceptTypeConvertersRegistrar.EnsureForEntryAssembly();

                KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                    silo,
                    chronicleBuilder =>
                        (configureStorage ?? (cb => cb.WithMongoDB(mongoServer, Constants.EventStore)))(chronicleBuilder));

                silo.AddActivityPropagation();

                silo.ConfigureServices(services =>
                {
                    // Remove the ChronicleServerStartupTask that AddChronicleToSilo registered.
                    // In tests, databases are fresh for each test and the fixture handles all
                    // setup (artifact registration, namespace creation, etc.) itself.
                    // Keeping the startup task causes a deadlock: PatchManager grain activation
                    // can hang when the silo is starting for the first time while other test
                    // infrastructure is being initialized concurrently.
                    var startupTaskType = typeof(KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions).Assembly
                        .GetType("Orleans.Hosting.ChronicleServerStartupTask");
                    if (startupTaskType is not null)
                    {
                        foreach (var descriptor in services.Where(d => d.ImplementationType == startupTaskType).ToList())
                        {
                            services.Remove(descriptor);
                        }
                    }

                    services.AddTypeDiscovery();
                    services.AddBindingsByConvention();
                    services.AddSelfBindings();

                    services.AddSingleton<IReactorMediator, ReactorMediator>();
                    services.AddSingleton<IReducerMediator, ReducerMediator>();

                    // Use DelegatingClientArtifactsProvider so the shared silo can serve
                    // artifacts from whichever test fixture is currently active.
                    // RemoveAll ensures it wins over convention-based discovery.
                    services.RemoveAll<IClientArtifactsProvider>();
                    services.AddSingleton<IClientArtifactsProvider>(delegatingProvider);

                    services.AddSingleton<INamingPolicy>(new DefaultNamingPolicy());
                    services.AddSingleton<IIdentityProvider>(sp => new IdentityProvider(
                        sp.GetRequiredService<IHttpContextAccessor>(),
                        sp.GetRequiredService<ILogger<IdentityProvider>>()));
                    services.AddSingleton(Globals.JsonSerializerOptions);
                    services.AddHttpContextAccessor();

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

                        // Wrap the service provider with FallbackServiceProvider so that
                        // ChronicleClient.InitializeInternal (which creates a new
                        // ClientArtifactsActivator internally) uses a provider that can
                        // resolve per-test types from MutableServiceRegistry.
                        var registry = sp.GetRequiredService<MutableServiceRegistry>();
                        var wrappedSp = new FallbackServiceProvider(sp, registry);
                        return new ChronicleClient(connection, options, artifactsProvider, wrappedSp, identityProvider, loggerFactory: loggerFactory);
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

                    // Override the convention-registered ClientArtifactsActivator with
                    // TestClientArtifactsActivator so that artifact activation can
                    // resolve types from the MutableServiceRegistry.
                    services.RemoveAll<IClientArtifactsActivator>();
                    services.AddSingleton<IClientArtifactsActivator>(sp =>
                    {
                        var registry = sp.GetRequiredService<MutableServiceRegistry>();
                        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                        return new TestClientArtifactsActivator(sp, registry, loggerFactory);
                    });
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
