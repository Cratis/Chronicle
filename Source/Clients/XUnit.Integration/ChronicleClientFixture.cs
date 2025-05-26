// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.ServiceModel;
using Cratis.Types;
using DotNet.Testcontainers.Networks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents the base fixture.
/// </summary>
/// <typeparam name="TChronicleFixture">The type of the chroncile fixture.</typeparam>
public abstract class ChronicleClientFixture<TChronicleFixture> : IClientArtifactsProvider, IDisposable, IAsyncLifetime, IChronicleSetupFixture
    where TChronicleFixture : IChronicleFixture
{
    static readonly DefaultClientArtifactsProvider _defaultClientArtifactsProvider = new(new CompositeAssemblyProvider(ProjectReferencedAssemblies.Instance, PackageReferencedAssemblies.Instance));
    static PropertyInfo _servicesProperty = null!;
    static MethodInfo _createClientMethod = null!;
    static MethodInfo _createClientWithOptionsMethod = null!;
    static bool _isInitialized;

#pragma warning disable CA2213
    IAsyncDisposable? _webApplicationFactory;
#pragma warning restore CA2213
    bool _backupPerformed;
    string _name = string.Empty;
    IServiceProvider? _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClientFixture"/> class.
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
    public virtual bool AutoDiscoverArtifacts { get; }

    /// <summary>
    /// Gets the test <see cref="Assembly"/>.
    /// </summary>
    protected static Assembly? TestAssembly { get; private set; }

    /// <summary>
    /// Gets the content root of the tests.
    /// </summary>
    protected static ContentRoot? ContentRoot { get; private set; }

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
    public virtual IEnumerable<Type> Adapters => GetArtifactTypes(provider => provider.Adapters);

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
    public virtual IEnumerable<Type> Rules => GetArtifactTypes(provider => provider.Rules);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> AdditionalEventInformationProviders => GetArtifactTypes(provider => provider.AdditionalEventInformationProviders);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> AggregateRoots => GetArtifactTypes(provider => provider.AggregateRoots);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> AggregateRootStateTypes => GetArtifactTypes(provider => provider.AggregateRootStateTypes);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> ConstraintTypes => GetArtifactTypes(provider => provider.ConstraintTypes);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> UniqueConstraints => GetArtifactTypes(provider => provider.UniqueConstraints);

    /// <inheritdoc/>
    public virtual IEnumerable<Type> UniqueEventTypeConstraints => GetArtifactTypes(provider => provider.UniqueEventTypeConstraints);

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> for resolving services.
    /// </summary>
    public IServiceProvider Services => _services ??= EnsureInitialized(() => _servicesProperty.GetValue(_webApplicationFactory) as IServiceProvider)!;

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
            await (_webApplicationFactory?.DisposeAsync() ?? ValueTask.CompletedTask);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        });
    }

    /// <summary>
    /// Creates the WebApplicationFactory.
    /// </summary>
    /// <returns>The WebApplicationFactory as an <see cref="IAsyncDisposable"/>.</returns>
    protected abstract IAsyncDisposable CreateWebApplicationFactory();

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
/// <typeparam name="TChronicleFixture">The type of the chronicle fixture</typeparam>
/// <typeparam name="TFactory">The web application factory type.</typeparam>
/// <typeparam name="TStartup">The startup class type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleClientFixture{T, TF, TS}"/> class.
/// </remarks>
/// <param name="chronicleFixture"><see cref="ChronicleMongoDBFixture"/> to use.</param>
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
                return parameters.Length == 2 && parameters[0].ParameterType == typeof(IClientArtifactsProvider) && parameters[1].ParameterType == typeof(ContentRoot);
            }))
        {
            throw new ServiceActivationException($"{nameof(WebApplicationFactory<object>)} must have a public constructor that only takes {nameof(IClientArtifactsProvider)} and {nameof(ContentRoot)} parameters");
        }
        return (Activator.CreateInstance(webApplicationFactoryType, [this, ContentRoot]) as IAsyncDisposable)!;
    }
}
