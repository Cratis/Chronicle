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
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Setup;
using Cratis.DependencyInjection;
using Cratis.Json;
using Cratis.Serialization;
using Cratis.Traces;
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
/// <param name="storageHostConfiguration">Optional key-value pairs injected into <see cref="IConfiguration"/> so that <c>IOptions&lt;ChronicleOptions&gt;</c> picks up the correct storage type and connection string for non-MongoDB backends.</param>
/// <param name="contentRoot">The content root path.</param>
/// <typeparam name="TStartup">Type of the startup type.</typeparam>
/// <remarks>When deriving this class and overriding <see cref="ChronicleWebApplicationFactory{TStartup}.ConfigureWebHost"/> remember to call base.ConfigureWebHost.</remarks>
public class ChronicleOrleansInProcessWebApplicationFactory<TStartup>(
    IChronicleSetupFixture fixture,
    Action<IServiceCollection> configureServices,
    Action<IMongoDBBuilder> configureMongoDB,
    Action<IWebHostBuilder> configureWebHost,
    Action<Configuration.IChronicleBuilder>? configureStorage,
    Sinks.SinkTypeId? defaultSinkTypeId,
    IReadOnlyDictionary<string, string?>? storageHostConfiguration,
    ContentRoot contentRoot) : ChronicleWebApplicationFactory<TStartup>(fixture, contentRoot)
    where TStartup : class
{
    readonly IChronicleSetupFixture _fixture = fixture;

    /// <inheritdoc/>
    protected override IHostBuilder CreateHostBuilder()
    {
        var builder = Host.CreateDefaultBuilder();

        var mongoServer = storageHostConfiguration is null
            ? $"mongodb://localhost:{_fixture.MongoDBContainer.GetMappedPublicPort(27017)}/?directConnection=true"
            : "mongodb://localhost:27017/?directConnection=true";

        if (storageHostConfiguration is not null)
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(storageHostConfiguration));
        }

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
                services.AddChronicleTelemetry(ctx.Configuration);
                services.AddControllers();

                services.PostConfigure<ChronicleClientOptions>(options => options.EventStore = Constants.EventStore);
                services.PostConfigure<ChronicleAspNetCoreOptions>(options => options.EventStore = Constants.EventStore);

                // For SQL OOP modes the in-process silo has its own separate database and
                // therefore its own empty auth tables. The OOP Chronicle container owns the
                // canonical chronicle-dev-client registration and exposes its management
                // endpoint on host port 8081. Point the in-process ChronicleClient at that
                // port so it authenticates against the OOP container rather than against
                // the in-process silo's empty auth tables.
                if (storageHostConfiguration is not null)
                {
                    services.PostConfigure<ChronicleClientOptions>(options => options.ManagementPort = 8081);
                }

                if (defaultSinkTypeId is not null)
                {
                    services.PostConfigure<ChronicleClientOptions>(options => options.DefaultSinkTypeId = defaultSinkTypeId);
                    services.PostConfigure<ChronicleOptions>(options => options.DefaultSinkTypeId = defaultSinkTypeId);
                }

                var capturingCollection = new CapturingServiceCollection();
                configureServices(capturingCollection);
                var testServiceRegistry = new MutableServiceRegistry();
                testServiceRegistry.Update(capturingCollection);
                services.AddSingleton(testServiceRegistry);

                // Register delegate factories that always resolve from MutableServiceRegistry so
                // that updates between tests are picked up by subsequent resolutions.
                foreach (var descriptor in capturingCollection)
                {
                    services.AddTransient(descriptor.ServiceType, sp =>
                    {
                        var registry = sp.GetRequiredService<MutableServiceRegistry>();
                        return registry.TryGet(descriptor.ServiceType, sp)
                            ?? ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType ?? descriptor.ServiceType);
                    });
                }
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

                // Convention binding must run BEFORE WithMongoDB/WithSql so the explicit storage
                // registrations win in DI. AddBindingsByConvention scans both Storage.MongoDB and
                // Storage.Sql assemblies and registers their classes by convention; if those
                // registrations come last, they shadow the explicit ones and grain activations
                // fail with "Unable to resolve ITableMigrator<>" because the inactive backend's
                // services have no infrastructure wired up. Use silo.Services directly because
                // silo.ConfigureServices QUEUES the action — it would run later than the
                // IMMEDIATE registrations performed inside AddChronicleToSilo's WithMongoDB call.
                silo.Services.AddTypeDiscovery();
                silo.Services.AddBindingsByConvention();
                silo.Services.AddSelfBindings();

                KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions.AddChronicleToSilo(
                    silo,
                    chronicleBuilder =>
                        (configureStorage ?? (cb => cb.WithMongoDB(mongoServer, Constants.EventStore)))(chronicleBuilder));

                silo.AddActivityPropagation();

                if (storageHostConfiguration is not null)
                {
                    silo.ConfigureServices(services =>
                        services
                            .AddOptions<Configuration.ChronicleOptions>()
                            .BindConfiguration(Configuration.ChronicleOptions.SectionPath));
                }

                silo.ConfigureServices(services =>
                {
                    // The ChronicleServerStartupTask deadlocks during test silo startup because
                    // PatchManager grain activation hangs while other test infrastructure is
                    // initializing concurrently. Tests handle their own setup via the fixture.
                    var startupTaskType = typeof(KernelCore::Orleans.Hosting.ChronicleServerSiloBuilderExtensions).Assembly
                        .GetType("Orleans.Hosting.ChronicleServerStartupTask");
                    if (startupTaskType is not null)
                    {
                        foreach (var descriptor in services.Where(d => d.ImplementationType == startupTaskType).ToList())
                        {
                            services.Remove(descriptor);
                        }
                    }

                    services.AddSingleton<IReactorMediator, ReactorMediator>();
                    services.AddSingleton<IReducerMediator, ReducerMediator>();

                    // The shared silo serves artifacts from whichever test fixture is currently
                    // active; RemoveAll ensures this wins over convention-based discovery.
                    services.RemoveAll<IClientArtifactsProvider>();
                    services.AddSingleton<IClientArtifactsProvider>(delegatingProvider);

                    services.AddSingleton<INamingPolicy>(new DefaultNamingPolicy());
                    services.AddSingleton<IIdentityProvider>(sp => new IdentityProvider(
                        sp.GetRequiredService<IHttpContextAccessor>(),
                        sp.GetRequiredService<ILogger<IdentityProvider>>()));
                    services.AddSingleton(Globals.JsonSerializerOptions);
                    services.AddHttpContextAccessor();
                    services.AddNamedActivitySource(ClientActivity.SourceName);
                    for (var index = services.Count - 1; index >= 0; index--)
                    {
                        var descriptor = services[index];
                        if (descriptor.ServiceType == typeof(IActivitySource<>) &&
                            Equals(descriptor.ServiceKey, ClientActivity.SourceName))
                        {
                            services.RemoveAt(index);
                            break;
                        }
                    }
                    services.AddKeyedSingleton<IActivitySource<EventSequences.EventSequence>>(ClientActivity.SourceName, (sp, key) =>
                        new ActivitySource<EventSequences.EventSequence>(sp.GetRequiredKeyedService<System.Diagnostics.ActivitySource>(key)));
                    services.AddKeyedSingleton<IActivitySource<Reactors.Reactors>>(ClientActivity.SourceName, (sp, key) =>
                        new ActivitySource<Reactors.Reactors>(sp.GetRequiredKeyedService<System.Diagnostics.ActivitySource>(key)));
                    services.AddKeyedSingleton<IActivitySource<Reducers.Reducers>>(ClientActivity.SourceName, (sp, key) =>
                        new ActivitySource<Reducers.Reducers>(sp.GetRequiredKeyedService<System.Diagnostics.ActivitySource>(key)));

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

                    // TestClientArtifactsActivator wraps the service provider with a fallback that
                    // resolves per-test types from MutableServiceRegistry. RemoveAll ensures this
                    // wins over any convention-registered IClientArtifactsActivator.
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
