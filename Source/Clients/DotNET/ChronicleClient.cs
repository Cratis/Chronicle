// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Aggregates;
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

    readonly IChronicleConnection? _connection;
    readonly ChronicleOptions _options;
    readonly ICausationManager _causationManager;
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
        _options = options;
        var result = Initialize();
        _causationManager = result.CausationManager;
        _jsonSchemaGenerator = result.JsonSchemaGenerator;

        var connectionLifecycle = new ConnectionLifecycle(options.LoggerFactory.CreateLogger<ConnectionLifecycle>());
        _connection = new ChronicleConnection(
            options,
            connectionLifecycle,
            new Tasks.Tasks(),
            options.LoggerFactory.CreateLogger<ChronicleConnection>(),
            CancellationToken.None);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleClient"/> class.
    /// </summary>
    /// <param name="connection"><see cref="IChronicleConnection"/> to use.</param>
    /// <param name="options">Optional <see cref="ChronicleOptions"/>.</param>
    public ChronicleClient(IChronicleConnection connection, ChronicleOptions options)
    {
        _options = options;
        var result = Initialize();
        _causationManager = result.CausationManager;
        _jsonSchemaGenerator = result.JsonSchemaGenerator;
        _connection = connection;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection?.Dispose();
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

        eventStore = new EventStore(
            name,
            @namespace,
            _connection!,
            _options.ArtifactsProvider,
            _causationManager,
            _options.IdentityProvider,
            _jsonSchemaGenerator,
            _options.ModelNameConvention,
            _options.ServiceProvider,
            _options.JsonSerializerOptions,
            _options.LoggerFactory);

        _eventStores[key] = eventStore;
        return eventStore;
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<EventStoreName> ListEventStores(CancellationToken cancellationToken = default) => throw new NotImplementedException();

    (ICausationManager CausationManager, IJsonSchemaGenerator JsonSchemaGenerator) Initialize()
    {
        var causationManager = new CausationManager();
        causationManager.DefineRoot(new Dictionary<string, string>
        {
            { VersionMetadataKey, _options.SoftwareVersion },
            { CommitMetadataKey, _options.SoftwareCommit },
            { ProgramIdentifierMetadataKey, _options.ProgramIdentifier },
            { OperatingSystemMetadataKey, Environment.OSVersion.ToString() },
            { MachineNameMetadataKey, Environment.MachineName },
            { ProcessMetadataKey, Environment.ProcessPath ?? string.Empty }
        });

        var complianceMetadataResolver = new ComplianceMetadataResolver(
            new InstancesOf<ICanProvideComplianceMetadataForType>(Types.Types.Instance, _options.ServiceProvider),
            new InstancesOf<ICanProvideComplianceMetadataForProperty>(Types.Types.Instance, _options.ServiceProvider));
        var jsonSchemaGenerator = new JsonSchemaGenerator(complianceMetadataResolver);

        return (causationManager, jsonSchemaGenerator);
    }

    record EventStoreKey(EventStoreName Name, EventStoreNamespaceName Namespace);
}
