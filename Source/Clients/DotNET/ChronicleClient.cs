// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    readonly ChronicleOptions _options;
    readonly IChronicleConnection? _connection;
    readonly ICausationManager _causationManager;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly IComplianceMetadataResolver _complianceMetadataResolver;
    readonly IConnectionLifecycle _connectionLifecycle;

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
        var causationManager = new CausationManager();
        causationManager.DefineRoot(new Dictionary<string, string>
        {
            { VersionMetadataKey, options.SoftwareVersion },
            { CommitMetadataKey, options.SoftwareCommit },
            { ProgramIdentifierMetadataKey, options.ProgramIdentifier },
            { OperatingSystemMetadataKey, Environment.OSVersion.ToString() },
            { MachineNameMetadataKey, Environment.MachineName },
            { ProcessMetadataKey, Environment.ProcessPath ?? string.Empty }
        });

        _causationManager = causationManager;

        _complianceMetadataResolver = new ComplianceMetadataResolver(
            new InstancesOf<ICanProvideComplianceMetadataForType>(Types.Types.Instance, options.ServiceProvider),
            new InstancesOf<ICanProvideComplianceMetadataForProperty>(Types.Types.Instance, options.ServiceProvider));
        _jsonSchemaGenerator = new JsonSchemaGenerator(_complianceMetadataResolver);
        _connectionLifecycle = new ConnectionLifecycle(options.LoggerFactory.CreateLogger<ConnectionLifecycle>());
        _connection = new ChronicleConnection(
            options,
            _connectionLifecycle,
            new Tasks.Tasks(),
            options.LoggerFactory.CreateLogger<ChronicleConnection>(),
            CancellationToken.None);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _connection?.Dispose();
    }

    /// <inheritdoc/>
    public IEventStore GetEventStore(EventStoreName name, EventStoreNamespaceName? @namespace = null) =>
        new EventStore(
            name,
            @namespace ?? EventStoreNamespaceName.Default,
            _connection!,
            _options.ArtifactsProvider,
            _causationManager,
            _options.IdentityProvider,
            _jsonSchemaGenerator,
            _options.ModelNameConvention,
            _options.ServiceProvider,
            _options.JsonSerializerOptions,
            _options.LoggerFactory);

    /// <inheritdoc/>
    public IAsyncEnumerable<EventStoreName> ListEventStores(CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
