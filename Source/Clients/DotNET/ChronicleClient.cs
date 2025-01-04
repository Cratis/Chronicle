// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Schemas;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IChronicleClient"/>.
/// </summary>
public class ChronicleClient : IChronicleClient, IDisposable
{
    const string VersionMetadataKey = "softwareVersion";
    const string CommitMetadataKey = "softwareCommit";
    const string ProgramIdentifierMetadataKey = "programIdentifier";
    const string OperatingSystemMetadataKey = "os";
    const string MachineNameMetadataKey = "machineName";
    const string ProcessMetadataKey = "process";

    readonly IChronicleConnection _connection;
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly ConcurrentDictionary<EventStoreKey, IEventStore> _eventStores = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="connectionString">Connection string to use.</param>
    public ChronicleClient(string connectionString)
        : this(new ChronicleUrl(connectionString))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="url"><see cref="ChronicleUrl"/> to connect with.</param>
    public ChronicleClient(ChronicleUrl url)
        : this(ChronicleOptions.FromUrl(url))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="options"><see cref="ChronicleOptions"/> to use.</param>
    public ChronicleClient(ChronicleOptions options)
    {
        Options = options;
        var result = Initialize();
        CausationManager = result.CausationManager;
        _jsonSchemaGenerator = result.JsonSchemaGenerator;

        var connectionLifecycle = new ConnectionLifecycle(options.LoggerFactory.CreateLogger<ConnectionLifecycle>());
        _connection = new ChronicleConnection(
            options.Url,
            options.ConnectTimeout,
            connectionLifecycle,
            new Cratis.Tasks.TaskFactory(),
            options.LoggerFactory.CreateLogger<ChronicleConnection>(),
            CancellationToken.None);
        _servicesAccessor = (_connection as IChronicleServicesAccessor)!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="connection"><see cref="IChronicleConnection"/> to use.</param>
    /// <param name="options">Optional <see cref="ChronicleOptions"/>.</param>
    public ChronicleClient(IChronicleConnection connection, ChronicleOptions options)
    {
        Options = options;
        var result = Initialize();
        CausationManager = result.CausationManager;
        _jsonSchemaGenerator = result.JsonSchemaGenerator;
        _connection = connection;
        _servicesAccessor = (_connection as IChronicleServicesAccessor)!;
    }

    /// <inheritdoc/>
    public ChronicleOptions Options { get; }

    /// <inheritdoc/>
    public ICausationManager CausationManager { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection.Dispose();
    }

    /// <inheritdoc/>
    public IEventStore GetEventStore(EventStoreName name, EventStoreNamespaceName? @namespace = null)
    {
        @namespace ??= EventStoreNamespaceName.Default;
        var key = new EventStoreKey(name, @namespace);
        if (_eventStores.TryGetValue(key, out var eventStore))
        {
            return eventStore;
        }

        Options.ArtifactsProvider.Initialize();

        eventStore = new EventStore(
            name,
            @namespace,
            _connection,
            Options.ArtifactsProvider,
            Options.CorrelationIdAccessor,
            CausationManager,
            Options.IdentityProvider,
            _jsonSchemaGenerator,
            Options.ModelNameConvention,
            Options.ServiceProvider,
            Options.JsonSerializerOptions,
            Options.LoggerFactory);

        _eventStores[key] = eventStore;
        return eventStore;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores(CancellationToken cancellationToken = default)
    {
        var eventStores = await _servicesAccessor.Services.EventStores.GetEventStores();
        return eventStores.Select(_ => (EventStoreName)_).ToArray();
    }

    (ICausationManager CausationManager, IJsonSchemaGenerator JsonSchemaGenerator) Initialize()
    {
        var causationManager = new CausationManager();
        causationManager.DefineRoot(new Dictionary<string, string>
        {
            { VersionMetadataKey, Options.SoftwareVersion },
            { CommitMetadataKey, Options.SoftwareCommit },
            { ProgramIdentifierMetadataKey, Options.ProgramIdentifier },
            { OperatingSystemMetadataKey, Environment.OSVersion.ToString() },
            { MachineNameMetadataKey, Environment.MachineName },
            { ProcessMetadataKey, Environment.ProcessPath ?? string.Empty }
        });

        var complianceMetadataResolver = new ComplianceMetadataResolver(
            new InstancesOf<ICanProvideComplianceMetadataForType>(Types.Types.Instance, Options.ServiceProvider),
            new InstancesOf<ICanProvideComplianceMetadataForProperty>(Types.Types.Instance, Options.ServiceProvider));
        var jsonSchemaGenerator = new JsonSchemaGenerator(complianceMetadataResolver);

        return (causationManager, jsonSchemaGenerator);
    }

    record EventStoreKey(EventStoreName Name, EventStoreNamespaceName Namespace);
}
