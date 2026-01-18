// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Serialization;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents the settings for connecting to Chronicle.
/// </summary>
/// <param name="connectionString"><see cref="ChronicleConnectionString"/> to use.</param>
/// <param name="namingPolicy">Optional <see cref="INamingPolicy"/> to use for converting names of types and properties.</param>
/// <param name="identityProvider">Optional <see cref="IIdentityProvider"/> to use. Will revert to default if not configured.</param>
/// <param name="jsonSerializerOptions">Optional <see cref="JsonSerializerOptions"/> to use. Will revert to defaults if not configured.</param>
/// <param name="serviceProvider">Optional <see cref="IServiceProvider"/> for resolving instances of things like event types, Reactors, reducers, projections and other artifacts. Will revert to <see cref="DefaultServiceProvider"/> if not configured.</param>
/// <param name="artifactsProvider">Optional <see cref="IClientArtifactsProvider"/>. If not specified, it will use the <see cref="DefaultClientArtifactsProvider"/> with both project and package referenced assemblies.</param>
/// <param name="correlationIdAccessor">Optional <see cref="ICorrelationIdAccessor"/> to use. Will revert to default if not configured.</param>
/// <param name="eventStoreNamespaceResolver">Optional <see cref="IEventStoreNamespaceResolver"/> to use. Will revert to default if not configured.</param>
/// <param name="concurrencyOptions">Optional <see cref="ConcurrencyOptions"/> to use. Will revert to default if not configured.</param>
/// <param name="autoDiscoverAndRegister">Optional disable automatic discovery of artifacts and registering these.</param>
/// <param name="connectTimeout">Optional timeout when connecting in seconds. Defaults to 5.</param>
/// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/> to use internally in client for logging.</param>
public class ChronicleOptions(
    ChronicleConnectionString connectionString,
    INamingPolicy? namingPolicy = null,
    IIdentityProvider? identityProvider = null,
    JsonSerializerOptions? jsonSerializerOptions = null,
    IServiceProvider? serviceProvider = null,
    IClientArtifactsProvider? artifactsProvider = null,
    ICorrelationIdAccessor? correlationIdAccessor = null,
    IEventStoreNamespaceResolver? eventStoreNamespaceResolver = null,
    ConcurrencyOptions? concurrencyOptions = null,
    bool autoDiscoverAndRegister = true,
    int connectTimeout = 5,
    ILoggerFactory? loggerFactory = null)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleOptions"/> class.
    /// </summary>
    public ChronicleOptions() : this(ChronicleConnectionString.Default)
    {
    }

    /// <summary>
    /// Gets the <see cref="ChronicleConnectionString"/> to use.
    /// </summary>
    public ChronicleConnectionString ConnectionString { get; set; } = connectionString;

    /// <summary>
    /// Gets or sets the software version.
    /// </summary>
    public string SoftwareVersion { get; set; } = VersionInformation.GetVersion();

    /// <summary>
    /// Gets or sets the software commit.
    /// </summary>
    public string SoftwareCommit { get; set; } = VersionInformation.GetCommitSha();

    /// <summary>
    /// Gets or sets the program identifier.
    /// </summary>
    public string ProgramIdentifier { get; set; } = "[N/A]";

    /// <summary>
    /// Gets the <see cref="IIdentityProvider"/> to use.
    /// </summary>
    public IIdentityProvider IdentityProvider { get; set; } = identityProvider ?? new BaseIdentityProvider();

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> to use.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = jsonSerializerOptions ?? new JsonSerializerOptions();

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> to use.
    /// </summary>
    public IServiceProvider ServiceProvider { get; set; } = serviceProvider ?? new DefaultServiceProvider();

    /// <summary>
    /// Gets the <see cref="IClientArtifactsProvider"/> to use.
    /// </summary>
    public IClientArtifactsProvider ArtifactsProvider { get; set; } = artifactsProvider ?? new DefaultClientArtifactsProvider(new CompositeAssemblyProvider(ProjectReferencedAssemblies.Instance, PackageReferencedAssemblies.Instance));

    /// <summary>
    /// Gets the <see cref="ICorrelationIdAccessor"/> to use.
    /// </summary>
    public ICorrelationIdAccessor CorrelationIdAccessor { get; set; } = correlationIdAccessor ?? new CorrelationIdAccessor();

    /// <summary>
    /// Gets the <see cref="IEventStoreNamespaceResolver"/> to use.
    /// </summary>
    public IEventStoreNamespaceResolver EventStoreNamespaceResolver { get; set; } = eventStoreNamespaceResolver ?? new DefaultEventStoreNamespaceResolver();

    /// <summary>
    /// Gets the <see cref="ConcurrencyOptions"/> to use for concurrency management.
    /// </summary>
    public ConcurrencyOptions ConcurrencyOptions { get; set; } = concurrencyOptions ?? new ConcurrencyOptions();

    /// <summary>
    /// Gets the <see cref="INamingPolicy"/> to use.
    /// </summary>
    public INamingPolicy NamingPolicy { get; set; } = namingPolicy ?? new DefaultNamingPolicy();

    /// <summary>
    /// Gets a value indicating whether to automatically discover and register artifacts.
    /// </summary>
    public bool AutoDiscoverAndRegister { get; set; } = autoDiscoverAndRegister;

    /// <summary>
    /// Gets the timeout when connecting in seconds.
    /// </summary>
    public int ConnectTimeout { get; set; } = connectTimeout;

    /// <summary>
    /// Gets or sets the maximum receive message size in bytes for gRPC messages. Defaults to 100 MB.
    /// </summary>
    public int? MaxReceiveMessageSize { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum send message size in bytes for gRPC messages. Defaults to 100 MB.
    /// </summary>
    public int? MaxSendMessageSize { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Gets the <see cref="ILoggerFactory"/> to use internally in the client.
    /// </summary>
    public ILoggerFactory LoggerFactory { get; set; } = loggerFactory ?? new LoggerFactory();

    /// <summary>
    /// Gets or sets the claim type to use for claims-based namespace resolution.
    /// This is used by the <see cref="ClaimsBasedNamespaceResolver"/> when configured via the WithClaimsBasedNamespaceResolver extension method.
    /// </summary>
    public string ClaimsBasedNamespaceResolverClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// Gets or sets the TLS configuration.
    /// </summary>
    public Tls Tls { get; set; } = new Tls();

    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    public Authentication Authentication { get; set; } = new Authentication();

    /// <summary>
    /// Gets or sets the port for the Management API and well-known certificate endpoint.
    /// </summary>
    public int ManagementPort { get; set; } = 8080;

    /// <summary>
    /// Create a <see cref="ChronicleOptions"/> from a connection string.
    /// </summary>
    /// <param name="connectionString">Connection string to create from.</param>
    /// <returns>A new <see cref="ChronicleOptions"/>.</returns>
    public static ChronicleOptions FromConnectionString(string connectionString) => FromConnectionString(new ChronicleConnectionString(connectionString));

    /// <summary>
    /// Create a <see cref="ChronicleOptions"/> from a <see cref="ChronicleConnectionString"/>.
    /// </summary>
    /// <param name="connectionString"><see cref="ChronicleConnectionString"/> to create from.</param>
    /// <returns>A new <see cref="ChronicleOptions"/>.</returns>
    public static ChronicleOptions FromConnectionString(ChronicleConnectionString connectionString) => new(connectionString)
    {
        Authentication = new Authentication()
    };
}
