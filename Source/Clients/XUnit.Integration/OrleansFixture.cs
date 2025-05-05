// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.Grains;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.DependencyInjection;
using Cratis.Json;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Storage;
using Orleans.TestingHost.Logging;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a fixture for Orleans integration tests.
/// </summary>
/// <param name="globalFixture">The global <see cref="ChronicleFixture"/>.</param>
public class OrleansFixture(ChronicleFixture globalFixture) : WebApplicationFactory<Startup>, IClientArtifactsProvider
{
    bool _backupPerformed;
    string _name = string.Empty;

    /// <summary>
    /// Gets the docker network.
    /// </summary>
    public INetwork Network => ChronicleFixture.Network;

    /// <summary>
    /// Gets the event store database.
    /// </summary>
    public MongoDBDatabase EventStoreDatabase => ChronicleFixture.EventStore;

    /// <summary>
    /// Gets the event store database for the namespace used in the event store.
    /// </summary>
    public MongoDBDatabase EventStoreForNamespaceDatabase => ChronicleFixture.EventStoreForNamespace;

    /// <summary>
    /// Gets the read models database.
    /// </summary>
    public MongoDBDatabase ReadModelsDatabase => ChronicleFixture.ReadModels;

    /// <summary>
    /// Gets the event store.
    /// </summary>
    public IEventStore EventStore => Services.GetRequiredService<IEventStore>();

    /// <summary>
    /// Gets the <see cref="IChronicleClient"/>.
    /// </summary>
    public IChronicleClient ChronicleClient => Services.GetRequiredService<IChronicleClient>();

    /// <summary>
    /// Gets the <see cref="ChronicleFixture"/>.
    /// </summary>
    public ChronicleFixture ChronicleFixture { get; } = globalFixture;

    /// <inheritdoc/>
    public virtual IEnumerable<Type> EventTypes { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Projections { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Adapters { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Reactors { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Reducers { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ReactorMiddlewares { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ComplianceForTypesProviders { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ComplianceForPropertiesProviders { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Rules { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> AdditionalEventInformationProviders { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> AggregateRoots { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> AggregateRootStateTypes { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ConstraintTypes { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> UniqueConstraints { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> UniqueEventTypeConstraints { get; } = [];

    /// <summary>
    /// Gets the <see cref="IGrainFactory"/> for the Orleans silo.
    /// </summary>
    internal IGrainFactory GrainFactory => Services.GetRequiredService<IGrainFactory>();

    /// <summary>
    /// Internal: Gets the <see cref="IEventSequence"/> for the event log.
    /// </summary>
    /// <returns>The <see cref="IEventSequence"/>.</returns>
    internal IEventSequence EventLogSequenceGrain => GetEventSequenceGrain(EventSequenceId.Log);

    /// <summary>
    /// Internal: Gets the <see cref="IEventStoreStorage"/> for the event store.
    /// </summary>
    internal IEventStoreStorage EventStoreStorage => Services.GetRequiredService<IStorage>().GetEventStore(Constants.EventStore);

    /// <summary>
    /// Sets the name of the fixture.
    /// </summary>
    /// <param name="name">Name for the fixture.</param>
    public void SetName(string name) => _name = name;

    /// <summary>
    /// Initializes the fixture.
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    /// Internal: Gets the <see cref="IEventStoreNamespaceStorage"/> for the event store namespace.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>The <see cref="IEventStoreNamespaceStorage"/>.</returns>
    internal IEventStoreNamespaceStorage GetEventStoreNamespaceStorage(Concepts.EventStoreNamespaceName? namespaceName = null) => EventStoreStorage.GetNamespace(namespaceName ?? Concepts.EventStoreNamespaceName.Default);

    /// <summary>
    /// Internal: Gets the <see cref="IEventSequenceStorage"/> for the event log.
    /// </summary>
    /// <param name="namespaceName">The namespace name.</param>
    /// <returns>The <see cref="IEventSequenceStorage"/>.</returns>
    internal IEventSequenceStorage GetEventLogStorage(Concepts.EventStoreNamespaceName? namespaceName = null) => GetEventStoreNamespaceStorage(namespaceName).GetEventSequence(EventSequenceId.Log);

    /// <summary>
    /// Gets the <see cref="IGrainStorage"/> for the specified key.
    /// </summary>
    /// <typeparam name="TStorage">The type of the storage.</typeparam>
    /// <param name="key">The key of the storage.</param>
    /// <returns>The <see cref="IGrainStorage"/>.</returns>
    internal TStorage GetGrainStorage<TStorage>(string key)
        where TStorage : IGrainStorage => (TStorage)Services.GetRequiredKeyedService<IGrainStorage>(key);

    /// <summary>
    /// Internal: Gets the <see cref="EventSequencesStorageProvider"/> for the event sequences.
    /// </summary>
    /// <returns>The <see cref="EventSequencesStorageProvider"/>.</returns>
    internal EventSequencesStorageProvider GetEventSequenceStatesStorage() => GetGrainStorage<EventSequencesStorageProvider>(WellKnownGrainStorageProviders.EventSequences);

    /// <summary>
    /// Internal: Gets the <see cref="IEventSequence"/> for the specified event sequence ID.
    /// </summary>
    /// <param name="id">The event sequence ID.</param>
    /// <returns>The <see cref="IEventSequence"/>.</returns>
    internal IEventSequence GetEventSequenceGrain(EventSequenceId id) => Services.GetRequiredService<IGrainFactory>().GetGrain<IEventSequence>(CreateEventSequenceKey(id));

    /// <summary>
    /// Internal: Creates an <see cref="EventSequenceKey"/> for the specified event sequence ID.
    /// </summary>
    /// <param name="id">The event sequence ID.</param>
    /// <returns>The <see cref="EventSequenceKey"/>.</returns>
    internal EventSequenceKey CreateEventSequenceKey(EventSequenceId id) => new(id, Constants.EventStore, Concepts.EventStoreNamespaceName.Default);

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
                services.Configure<ChronicleOptions>(opts => opts.ArtifactsProvider = this);

                ConfigureServices(services);
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

        // For some weird reason we need this https://stackoverflow.com/questions/69974249/no-app-configured-error-while-using-webapplicationfactory-for-running-integrat
        builder.ConfigureWebHostDefaults(b => b.Configure(app => { }));
        return builder;
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSolutionRelativeContentRoot("Integration/Orleans.InProcess");
    }

    /// <summary>
    /// Ensures that the event store is built.
    /// </summary>
    protected void EnsureBuilt()
    {
        Services.GetRequiredService<IEventStore>();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (!_backupPerformed)
        {
            ChronicleFixture.PerformBackup(_name);
            _backupPerformed = true;
        }

        ChronicleFixture.RemoveAllDatabases().GetAwaiter().GetResult();
        base.Dispose(false);
    }

    /// <summary>
    /// Overridable method to configure services.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to configure.</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
}
