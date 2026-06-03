// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle;

/// <summary>
/// Represents the settings for connecting to Chronicle.
/// </summary>
/// <remarks>
/// Using the default constructor initializes the options with the development connection string
/// (<see cref="ChronicleConnectionString.Development" />), which includes the default development
/// client credentials. This is intended for local development and testing only.
/// </remarks>
/// <param name="connectionString"><see cref="ChronicleConnectionString"/> to use.</param>
/// <param name="jsonSerializerOptions">Optional <see cref="JsonSerializerOptions"/> to use. Will revert to defaults if not configured.</param>
/// <param name="concurrencyOptions">Optional <see cref="ConcurrencyOptions"/> to use. Will revert to default if not configured.</param>
/// <param name="autoDiscoverAndRegister">Optional disable automatic discovery of artifacts and registering these.</param>
/// <param name="connectTimeout">Optional timeout when connecting in seconds. Defaults to 5.</param>
public class ChronicleOptions(
    ChronicleConnectionString connectionString,
    JsonSerializerOptions? jsonSerializerOptions = null,
    ConcurrencyOptions? concurrencyOptions = null,
    bool autoDiscoverAndRegister = true,
    int connectTimeout = 5)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleOptions"/> class.
    /// </summary>
    /// <remarks>
    /// This initializes the options with the development connection string
    /// (<see cref="ChronicleConnectionString.Development" />), which includes the default development
    /// client credentials.
    /// </remarks>
    public ChronicleOptions() : this(ChronicleConnectionString.Development)
    {
    }

    /// <summary>
    /// Gets the <see cref="ChronicleConnectionString"/> to use.
    /// </summary>
    public ChronicleConnectionString ConnectionString { get; set; } = connectionString;

    /// <summary>
    /// Gets or sets the software version.
    /// Defaults to the version of the entry assembly (your application), extracted from the
    /// AssemblyInformationalVersion or AssemblyVersion attribute.
    /// This is included in the root causation to track which version of your application is running.
    /// </summary>
    public string SoftwareVersion { get; set; } = VersionInformation.GetVersion();

    /// <summary>
    /// Gets or sets the software commit SHA.
    /// Defaults to the commit SHA from the entry assembly's (your application's)
    /// AssemblyInformationalVersion metadata (the portion after the '+' separator).
    /// This is included in the root causation to track which commit of your application is running.
    /// </summary>
    public string SoftwareCommit { get; set; } = VersionInformation.GetCommitSha();

    /// <summary>
    /// Gets or sets the program identifier.
    /// </summary>
    public string ProgramIdentifier { get; set; } = "[N/A]";

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> to use.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = jsonSerializerOptions ?? new JsonSerializerOptions();

    /// <summary>
    /// Gets the <see cref="ConcurrencyOptions"/> to use for concurrency management.
    /// </summary>
    public ConcurrencyOptions ConcurrencyOptions { get; set; } = concurrencyOptions ?? new ConcurrencyOptions();

    /// <summary>
    /// Gets a value indicating whether to automatically discover and register artifacts.
    /// </summary>
    public bool AutoDiscoverAndRegister { get; set; } = autoDiscoverAndRegister;

    /// <summary>
    /// Gets the timeout when connecting in seconds.
    /// </summary>
    public int ConnectTimeout { get; set; } = connectTimeout;

    /// <summary>
    /// Gets or sets a value indicating whether to skip the keep-alive handshake on connect.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default Chronicle client opens a bidirectional keep-alive stream right after connect.
    /// A background watchdog measures the time since the last server ping and, if it exceeds
    /// five seconds, treats the channel as dead — disposes the underlying gRPC channel, raises
    /// <see cref="IConnectionLifecycle.Disconnected"/>, and asks the connection to reconnect.
    /// This is exactly what a long-lived client wants: liveness detection that survives transient
    /// network glitches, kernel restarts, and silo reactivation.
    /// </para>
    /// <para>
    /// For short-lived, one-shot callers the watchdog is a hazard rather than a feature. A
    /// development-only kernel reset (<c>IServer.ResetKernelState</c>), a CLI that issues a
    /// single command, or any other client whose lifetime is a single RPC does not benefit from
    /// liveness detection and is actively harmed by it: a single RPC that takes longer than the
    /// five-second window (for example a SQL backend wipe that has to enumerate, connect to, and
    /// drop tables across every event store / namespace / read-model database) trips the
    /// watchdog mid-call, disposes the channel out from under the in-flight call, and surfaces
    /// as <see cref="ObjectDisposedException"/> on <c>Grpc.Net.Client.GrpcChannel</c>.
    /// </para>
    /// <para>
    /// Set this to <see langword="true"/> when the client will issue at most a handful of RPCs
    /// before being disposed, or when the longest RPC the client will issue can exceed five
    /// seconds of server-side processing. With keep-alive skipped the lifecycle is marked
    /// connected immediately after the gRPC channel is established, the watchdog is never
    /// started, and the channel survives for the full lifetime of the client.
    /// </para>
    /// </remarks>
    public bool SkipKeepAlive { get; set; }

    /// <summary>
    /// Gets or sets the maximum receive message size in bytes for gRPC messages. Defaults to 100 MB.
    /// </summary>
    public int? MaxReceiveMessageSize { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum send message size in bytes for gRPC messages. Defaults to 100 MB.
    /// </summary>
    public int? MaxSendMessageSize { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the claim type to use for claims-based namespace resolution.
    /// This is used by the <see cref="ClaimsBasedNamespaceResolver"/> when configured via the WithClaimsBasedNamespaceResolver extension method.
    /// </summary>
    public string ClaimsBasedNamespaceResolverClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// Gets or sets a value indicating whether event type generation validation is enabled.
    /// When <see langword="false"/> (the default), the client asks the Kernel to bypass migration
    /// chain validation when registering event types at generation 2 or higher. Set to
    /// <see langword="true"/> to opt into strict migration chain validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Kernel only honours this flag when running the development image.
    /// The production image always enforces validation unconditionally, regardless of the value sent by the client.
    /// </para>
    /// <para>
    /// The default is <see langword="false"/> so that early-stage schema development works without
    /// any explicit configuration. Set to <see langword="true"/> to opt into strict migration
    /// chain validation on the development Kernel image.
    /// </para>
    /// </remarks>
    public bool EnableEventTypeGenerationValidation { get; set; }

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
    /// Gets or sets the default <see cref="SinkTypeId"/> used when registering projections and reducers.
    /// When not explicitly configured, defaults to <see cref="WellKnownSinkTypes.MongoDB"/>.
    /// Set to <see cref="WellKnownSinkTypes.SQL"/> to persist read models into a SQL database.
    /// </summary>
    public SinkTypeId DefaultSinkTypeId { get; set; } = WellKnownSinkTypes.MongoDB;

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

    /// <summary>
    /// Create a <see cref="ChronicleOptions"/> from the development connection string.
    /// </summary>
    /// <returns>A new <see cref="ChronicleOptions"/> configured for development.</returns>
    /// <remarks>
    /// This is a convenience method for quickly creating options for development purposes. It uses the
    /// default development connection string which points to localhost with the default development
    /// client credentials. This is not intended for production use and should only be used for local
    /// development and testing. For production scenarios, it's recommended to explicitly configure the
    /// connection string and other options as needed.
    /// </remarks>
    public static ChronicleOptions FromDevelopmentConnectionString() => FromConnectionString(ChronicleConnectionString.Development);
}
