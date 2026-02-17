// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using System.Reflection;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Types;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Storage;
using Xunit;
using EventSequencesStorageProvider = KernelCore::Cratis.Chronicle.EventSequences.EventSequencesStorageProvider;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents the base fixture.
/// </summary>
/// <typeparam name="TChronicleFixture">The type of the Chronicle fixture.</typeparam>
public abstract class ChronicleClientFixture<TChronicleFixture> : IDisposable, IAsyncLifetime, IChronicleSetupFixture
    where TChronicleFixture : IChronicleFixture
{
#pragma warning disable CA2213, SA1600, CA1051
    protected IAsyncDisposable? _webApplicationFactory;
#pragma warning restore CA2213, SA1600, CA1051
    static readonly DefaultClientArtifactsProvider _defaultClientArtifactsProvider = new(new CompositeAssemblyProvider(ProjectReferencedAssemblies.Instance, PackageReferencedAssemblies.Instance));
    static PropertyInfo _servicesProperty = null!;
    static MethodInfo _createClientMethod = null!;
    static MethodInfo _createClientWithOptionsMethod = null!;
    static bool _isInitialized;
    bool _backupPerformed;
    string _name = string.Empty;
    IServiceProvider? _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClientFixture{T}"/> class.
    /// </summary>
    /// <param name="chronicleFixture"><see cref="ChronicleFixture"/> to use.</param>
    protected ChronicleClientFixture(TChronicleFixture chronicleFixture)
    {
        ChronicleFixture = chronicleFixture;

        TestAssembly ??= TestAssemblyLocator.GetTestAssembly();
        if (ContentRoot is null)
        {
            var codeBaseUri = new Uri(TestAssembly!.Location);
            ContentRoot = Path.GetDirectoryName(codeBaseUri.LocalPath)!;

            while (!Directory.EnumerateFiles(ContentRoot, "*.csproj").Any())
            {
                ContentRoot = Path.GetDirectoryName(ContentRoot)!;
            }
        }
    }

    /// <summary>
    /// Gets the value indicating whether to auto discover artifacts.
    /// </summary>
    public virtual bool AutoDiscoverArtifacts { get; } = true;

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
    public TChronicleFixture ChronicleFixture { get; }

    /// <inheritdoc/>
    public virtual IEnumerable<Type> EventTypes => GetArtifactTypes(provider => provider.EventTypes);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Projections => GetArtifactTypes(provider => provider.Projections);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ModelBoundProjections => GetArtifactTypes(provider => provider.ModelBoundProjections);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Reactors => GetArtifactTypes(provider => provider.Reactors);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> Reducers => GetArtifactTypes(provider => provider.Reducers);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ReactorMiddlewares => GetArtifactTypes(provider => provider.ReactorMiddlewares);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ComplianceForTypesProviders => GetArtifactTypes(provider => provider.ComplianceForTypesProviders);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ComplianceForPropertiesProviders => GetArtifactTypes(provider => provider.ComplianceForPropertiesProviders);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> AdditionalEventInformationProviders => GetArtifactTypes(provider => provider.AdditionalEventInformationProviders);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ConstraintTypes => GetArtifactTypes(provider => provider.ConstraintTypes);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> UniqueConstraints => GetArtifactTypes(provider => provider.UniqueConstraints);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> UniqueEventTypeConstraints => GetArtifactTypes(provider => provider.UniqueEventTypeConstraints);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> EventSeeders => GetArtifactTypes(provider => provider.EventSeeders);

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> for resolving services.
    /// </summary>
    public IServiceProvider Services => _services ??= EnsureInitialized(() => _servicesProperty.GetValue(_webApplicationFactory) as IServiceProvider)!;

    /// <summary>
    /// Gets the <see cref="IGrainFactory"/> for resolving grains.
    /// </summary>
    internal IGrainFactory GrainFactory => (this as IChronicleSetupFixture).GrainFactory;

    /// <summary>
    /// Gets the <see cref="KernelCore::Cratis.Chronicle.EventSequences.IEventSequence"/> grain for the event log sequence.
    /// </summary>
    internal KernelCore::Cratis.Chronicle.EventSequences.IEventSequence EventLogSequenceGrain => (this as IChronicleSetupFixture).EventLogSequenceGrain;

    /// <summary>
    /// Gets the <see cref="IEventStoreStorage"/> for the event store storage.
    /// </summary>
    internal IEventStoreStorage EventStoreStorage => (this as IChronicleSetupFixture).EventStoreStorage;

    /// <summary>
    /// Gets the test <see cref="Assembly"/>.
    /// </summary>
    protected static Assembly? TestAssembly { get; private set; }

    /// <summary>
    /// Gets the content root of the tests.
    /// </summary>
    protected static ContentRoot? ContentRoot { get; private set; }

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
    public HttpClient CreateClient() => EnsureInitialized(() => _createClientMethod.Invoke(_webApplicationFactory, null) as HttpClient)!;

    /// <summary>
    /// Create a new <see cref="HttpClient"/> instance.
    /// </summary>
    /// <param name="options">The <see cref="WebApplicationFactoryClientOptions"/>.</param>
    /// <returns>A new <see cref="HttpClient"/> instance.</returns>
    public HttpClient CreateClient(WebApplicationFactoryClientOptions options) => EnsureInitialized(() => _createClientWithOptionsMethod.Invoke(_webApplicationFactory, [options]) as HttpClient)!;

    /// <summary>
    /// Create a new <see cref="HttpClient"/> instance.
    /// </summary>
    /// <param name="options">The <see cref="WebApplicationFactoryClientOptions"/>.</param>
    /// <param name="handler">The <see cref="HttpMessageHandler"/> to use.</param>
    /// <returns>A new <see cref="HttpClient"/> instance.</returns>
    public HttpClient CreateClient(WebApplicationFactoryClientOptions options, HttpMessageHandler handler)
    {
        var client = CreateClient(options);
        var newClient = new HttpClient(handler)
        {
            BaseAddress = client.BaseAddress
        };
        client.Dispose();
        return newClient;
    }

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
    public virtual async Task DisposeAsync()
    {
        if (!_backupPerformed)
        {
            ChronicleFixture.PerformBackup(_name);
            _backupPerformed = true;
        }

        await ChronicleFixture.RemoveAllDatabases();
    }

    /// <summary>
    /// Gets the <see cref="IEventStoreNamespaceStorage"/> for the specified namespace.
    /// </summary>
    /// <param name="namespaceName">Namespace to get for.</param>
    /// <returns>The <see cref="IEventStoreNamespaceStorage"/> for the specified namespace.</returns>
    internal IEventStoreNamespaceStorage GetEventStoreNamespaceStorage(KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName? namespaceName = null) => (this as IChronicleSetupFixture).GetEventStoreNamespaceStorage(namespaceName);

    /// <summary>
    /// Gets the <see cref="IEventSequenceStorage"/> for the event log storage.
    /// </summary>
    /// <param name="namespaceName">Namespace to get for.</param>
    /// <returns>The <see cref="IEventSequenceStorage"/> for the event log storage.</returns>
    internal IEventSequenceStorage GetEventLogStorage(KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName? namespaceName = null) => (this as IChronicleSetupFixture).GetEventLogStorage(namespaceName);

    /// <summary>
    /// Gets the grain storage for the specified key.
    /// </summary>
    /// <typeparam name="TStorage">The type of the grain storage.</typeparam>
    /// <param name="key">The key for the grain storage.</param>
    /// <returns>The grain storage for the specified key.</returns>
    internal TStorage GetGrainStorage<TStorage>(string key)
        where TStorage : IGrainStorage => (this as IChronicleSetupFixture).GetGrainStorage<TStorage>(key);

    /// <summary>
    /// Gets the <see cref="EventSequencesStorageProvider"/> for the event sequence states storage.
    /// </summary>
    /// <returns>The <see cref="EventSequencesStorageProvider"/> for the event sequence states storage.</returns>
    internal EventSequencesStorageProvider GetEventSequenceStatesStorage() => (this as IChronicleSetupFixture).GetEventSequenceStatesStorage();

    /// <summary>
    /// Gets the <see cref="KernelCore::Cratis.Chronicle.EventSequences.IEventSequence"/> grain for the specified event sequence ID.
    /// </summary>
    /// <param name="id">The <see cref="KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId"/> for the event sequence.</param>
    /// <returns>The <see cref="KernelCore::Cratis.Chronicle.EventSequences.IEventSequence"/> grain for the specified event sequence ID.</returns>
    internal KernelCore::Cratis.Chronicle.EventSequences.IEventSequence GetEventSequenceGrain(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId id) => (this as IChronicleSetupFixture).GetEventSequenceGrain(id);

    /// <summary>
    /// Creates a new <see cref="KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey"/> for the specified event sequence ID.
    /// </summary>
    /// <param name="id">The <see cref="KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId"/> for the event sequence.</param>
    /// <returns>A new <see cref="KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey"/> for the specified event sequence ID.</returns>
    internal KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey CreateEventSequenceKey(KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId id) => (this as IChronicleSetupFixture).CreateEventSequenceKey(id);

    /// <summary>
    /// Creates the WebApplicationFactory.
    /// </summary>
    /// <returns>The WebApplicationFactory as an <see cref="IAsyncDisposable"/>.</returns>
    protected abstract IAsyncDisposable CreateWebApplicationFactory();

    /// <summary>
    /// Configure the WebHostBuilder.
    /// </summary>
    /// <param name="builder">The builder for continuation.</param>
    protected abstract void ConfigureWebHostBuilder(IWebHostBuilder builder);

    /// <summary>
    /// Ensures that the event store is built.
    /// </summary>
    protected void EnsureBuilt()
    {
        Services.GetRequiredService<IEventStore>();
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

    T EnsureInitialized<T>(Func<T> getObject)
    {
        if (_webApplicationFactory is null)
        {
            InitializeFixture();
        }
        return getObject();
    }

    void InitializeFixture()
    {
        _webApplicationFactory = CreateWebApplicationFactory();
        if (!_isInitialized)
        {
            var webApplicationFactoryType = _webApplicationFactory.GetType();
            _servicesProperty = webApplicationFactoryType.GetProperty(nameof(WebApplicationFactory<object>.Services), BindingFlags.Instance | BindingFlags.Public)!;
            _createClientMethod = webApplicationFactoryType.GetMethod(nameof(WebApplicationFactory<object>.CreateClient), BindingFlags.Instance | BindingFlags.Public, [])!;
            _createClientWithOptionsMethod = webApplicationFactoryType.GetMethod(nameof(WebApplicationFactory<object>.CreateClient), BindingFlags.Instance | BindingFlags.Public, [typeof(WebApplicationFactoryClientOptions)])!;
            _isInitialized = true;
        }
    }

    IEnumerable<Type> GetArtifactTypes(Func<DefaultClientArtifactsProvider, IEnumerable<Type>> getTypes)
    {
        if (!AutoDiscoverArtifacts)
        {
            return [];
        }
        _defaultClientArtifactsProvider.Initialize();
        return getTypes(_defaultClientArtifactsProvider);
    }
}

/// <summary>
/// Represents a fixture for Orleans integration tests.
/// </summary>
/// <typeparam name="TChronicleFixture">The type of the chronicle fixture.</typeparam>
/// <typeparam name="TFactory">The web application factory type.</typeparam>
/// <typeparam name="TStartup">The startup class type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleClientFixture{T, TF, TS}"/> class.
/// </remarks>
/// <param name="chronicleFixture"><see cref="ChronicleInProcessFixture"/> to use.</param>
#pragma warning disable SA1402
public class ChronicleClientFixture<TChronicleFixture, TFactory, TStartup>(TChronicleFixture chronicleFixture) : ChronicleClientFixture<TChronicleFixture>(chronicleFixture)
    where TChronicleFixture : IChronicleFixture
#pragma warning restore SA1402
    where TFactory : ChronicleWebApplicationFactory<TStartup>
    where TStartup : class
{
    /// <inheritdoc/>
    protected override IAsyncDisposable CreateWebApplicationFactory()
    {
        var webApplicationFactoryType = typeof(TFactory);
        if (!webApplicationFactoryType.GetConstructors().Any(_ =>
            {
                var parameters = _.GetParameters();
                return parameters.Length == 2 && parameters[0].ParameterType == typeof(IChronicleSetupFixture) && parameters[1].ParameterType == typeof(ContentRoot);
            }))
        {
            throw new InvalidWebApplicationFactory();
        }
        return (Activator.CreateInstance(webApplicationFactoryType, [this, ContentRoot]) as IAsyncDisposable)!;
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
    }
}
