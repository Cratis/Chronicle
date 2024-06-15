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
/// Represents an implementation of <see cref="ICratisClient"/>.
/// </summary>
public class CratisClient : ICratisClient, IDisposable
{
    const string VersionMetadataKey = "softwareVersion";
    const string CommitMetadataKey = "softwareCommit";
    const string ProgramIdentifierMetadataKey = "programIdentifier";
    const string OperatingSystemMetadataKey = "os";
    const string MachineNameMetadataKey = "machineName";
    const string ProcessMetadataKey = "process";

    readonly CratisOptions _options;
    readonly ICratisConnection? _connection;
    readonly ICausationManager _causationManager;
    readonly IJsonSchemaGenerator _jsonSchemaGenerator;
    readonly IComplianceMetadataResolver _complianceMetadataResolver;
    readonly IConnectionLifecycle _connectionLifecycle;

    /// <summary>
    /// Initializes a new instance of the <see cref="CratisClient"/> class.
    /// </summary>
    /// <param name="connectionString">Connection string to use.</param>
    public CratisClient(string connectionString)
        : this(new CratisUrl(connectionString))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CratisClient"/> class.
    /// </summary>
    /// <param name="url"><see cref="CratisUrl"/> to connect with.</param>
    public CratisClient(CratisUrl url)
        : this(CratisOptions.FromUrl(url))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CratisClient"/> class.
    /// </summary>
    /// <param name="options"><see cref="CratisOptions"/> to use.</param>
    public CratisClient(CratisOptions options)
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
        _connection = new CratisConnection(
            options,
            _connectionLifecycle,
            new Tasks.Tasks(),
            options.LoggerFactory.CreateLogger<CratisConnection>(),
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
