// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Serialization;

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
/// <param name="namingPolicy">Optional <see cref="INamingPolicy"/> to use for converting names of types and properties. Obsolete: configure on <see cref="IChronicleBuilder"/> instead.</param>
/// <param name="jsonSerializerOptions">Optional <see cref="JsonSerializerOptions"/> to use. Will revert to defaults if not configured.</param>
/// <param name="concurrencyOptions">Optional <see cref="ConcurrencyOptions"/> to use. Will revert to default if not configured.</param>
/// <param name="autoDiscoverAndRegister">Optional disable automatic discovery of artifacts and registering these.</param>
/// <param name="connectTimeout">Optional timeout when connecting in seconds. Defaults to 5.</param>
public class ChronicleOptions(
    ChronicleConnectionString connectionString,
    INamingPolicy? namingPolicy = null,
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
    /// Gets the <see cref="INamingPolicy"/> to use.
    /// </summary>
    /// <remarks>
    /// This property is obsolete. Configure the naming policy on the <see cref="IChronicleBuilder"/>
    /// using <c>builder.WithCamelCaseNamingPolicy()</c> or by setting <see cref="IChronicleBuilder.NamingPolicy"/> directly.
    /// </remarks>
    [Obsolete("Configure the naming policy on IChronicleBuilder using WithCamelCaseNamingPolicy() or by setting IChronicleBuilder.NamingPolicy directly.")]
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
    /// Gets or sets the claim type to use for claims-based namespace resolution.
    /// This is used by the <see cref="ClaimsBasedNamespaceResolver"/> when configured via the WithClaimsBasedNamespaceResolver extension method.
    /// </summary>
    public string ClaimsBasedNamespaceResolverClaimType { get; set; } = "tenant_id";

    /// <summary>
    /// Gets or sets a value indicating whether event type generation validation is disabled.
    /// When <see langword="true"/>, Chronicle will not enforce that every generation step
    /// from 1 to the current generation has a migration defined, and will not require
    /// sequential generation numbering.
    /// </summary>
    /// <remarks>
    /// This property is only honoured in DEVELOPMENT builds of the Kernel. In all other
    /// configurations the value sent to the server is ignored, ensuring that production
    /// deployments never silently skip migration chain validation.
    /// </remarks>
#if DEVELOPMENT
    public bool DisableEventTypeGenerationValidation { get; set; }
#else
    public bool DisableEventTypeGenerationValidation => false;
#endif

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
