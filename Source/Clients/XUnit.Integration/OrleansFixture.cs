// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grains;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Storage;
using Xunit;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a fixture for Orleans integration tests.
/// </summary>
public class OrleansFixture : IClientArtifactsProvider, IDisposable, IAsyncLifetime
{
    static Type _webApplicationFactoryType = null!;
    static PropertyInfo _servicesProperty = null!;
    static MethodInfo _createClientMethod = null!;
    static MethodInfo _createClientWithOptionsMethod = null!;
    static bool _isInitialized;
    static string _contentRoot = string.Empty;

    readonly IAsyncDisposable _webApplicationFactory;
    bool _backupPerformed;
    string _name = string.Empty;
    IServiceProvider? _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrleansFixture"/> class.
    /// </summary>
    /// <param name="chronicleFixture"><see cref="ChronicleFixture"/> to use.</param>
    public OrleansFixture(ChronicleFixture chronicleFixture)
    {
        ChronicleFixture = chronicleFixture;

        if (!_isInitialized)
        {
            var testAssembly = TestAssemblyLocator.GetTestAssembly();

            var codeBaseUri = new Uri(testAssembly!.Location);
            _contentRoot = Path.GetDirectoryName(codeBaseUri.LocalPath)!;

            while (!Directory.EnumerateFiles(_contentRoot, "*.csproj").Any())
            {
                _contentRoot = Path.GetDirectoryName(_contentRoot)!;
            }

            var startupType = testAssembly!.ExportedTypes.FirstOrDefault(type => type.Name == "Startup");
            startupType ??= testAssembly!.ExportedTypes.FirstOrDefault()!;
            _webApplicationFactoryType = typeof(ChronicleWebApplicationFactory<>).MakeGenericType(startupType!);
            _servicesProperty = _webApplicationFactoryType.GetProperty(nameof(ChronicleWebApplicationFactory<object>.Services), BindingFlags.Instance | BindingFlags.Public)!;
            _createClientMethod = _webApplicationFactoryType.GetMethod(nameof(ChronicleWebApplicationFactory<object>.CreateClient), BindingFlags.Instance | BindingFlags.Public, [])!;
            _createClientWithOptionsMethod = _webApplicationFactoryType.GetMethod(nameof(ChronicleWebApplicationFactory<object>.CreateClient), BindingFlags.Instance | BindingFlags.Public, [typeof(WebApplicationFactoryClientOptions)])!;

            _isInitialized = true;
        }

        var configureServices = ConfigureServices;
        _webApplicationFactory = (Activator.CreateInstance(_webApplicationFactoryType, [this, configureServices, _contentRoot]) as IAsyncDisposable)!;
    }

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
    public ChronicleFixture ChronicleFixture { get; }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> EventTypes { get; } = [];

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Projections { get; } = [];

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
    /// Gets the <see cref="IServiceProvider"/> for resolving services.
    /// </summary>
    public IServiceProvider Services => _services ??= (_servicesProperty.GetValue(_webApplicationFactory) as IServiceProvider)!;

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
    /// Create a new <see cref="HttpClient"/> instance.
    /// </summary>
    /// <returns>A new <see cref="HttpClient"/> instance.</returns>
    public HttpClient CreateClient() => (_createClientMethod.Invoke(_webApplicationFactory, null) as HttpClient)!;

    /// <summary>
    /// Create a new <see cref="HttpClient"/> instance.
    /// </summary>
    /// <param name="options">The <see cref="WebApplicationFactoryClientOptions"/>.</param>
    /// <returns>A new <see cref="HttpClient"/> instance.</returns>
    public HttpClient CreateClient(WebApplicationFactoryClientOptions options) => (_createClientMethod.Invoke(_webApplicationFactory, [options]) as HttpClient)!;

    /// <inheritdoc/>
    public virtual void Dispose()
    {
    }

    /// <inheritdoc/>
    public Task InitializeAsync()
    {
        return OnInitializeAsync();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        if (!_backupPerformed)
        {
            ChronicleFixture.PerformBackup(_name);
            _backupPerformed = true;
        }

        await ChronicleFixture.RemoveAllDatabases();
        _ = Task.Run(async () =>
        {
            await _webApplicationFactory.DisposeAsync();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        });
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

    /// <summary>
    /// Ensures that the event store is built.
    /// </summary>
    protected void EnsureBuilt()
    {
        Services.GetRequiredService<IEventStore>();
    }

    /// <summary>
    /// Overridable method to configure services.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to configure.</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Overridable method to perform actions when the fixture is disposed async.
    /// </summary>
    /// <returns>Awaitable Task.</returns>
    protected virtual Task OnInitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Overridable method to perform actions when the fixture is disposed async.
    /// </summary>
    /// <returns>Awaitable Task.</returns>
    protected virtual Task OnDisposeAsync() => Task.CompletedTask;
}
