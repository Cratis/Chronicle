// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the Chronicle options.
/// </summary>
public class ChronicleOptions
{
    /// <summary>
    /// Section paths for Chronicle configuration.
    /// </summary>
    public static readonly string[] SectionPaths = ["Cratis", "Chronicle"];

    /// <summary>
    /// Configuration path for the Chronicle section.
    /// </summary>
    public static readonly string SectionPath = ConfigurationPath.Combine(SectionPaths);

    /// <summary>
    /// Port to listen on for Chronicle traffic. With TLS enabled (the default) this single port multiplexes
    /// gRPC (HTTP/2) and the Workbench, API and OAuth flows (HTTP/1.1). When TLS is disabled it serves cleartext
    /// gRPC (h2c) only, and the HTTP/1.1 surface moves to <see cref="ManagementPort"/>.
    /// </summary>
    public int Port { get; init; } = 35000;

    /// <summary>
    /// Port for the plaintext HTTP/1.1 surface (Workbench, API, OAuth and health) used only when TLS is disabled
    /// (<see cref="Tls"/>.<see cref="Tls.Enabled"/> is <see langword="false"/>). Cleartext HTTP/1.1 and HTTP/2 cannot share a
    /// single port — ALPN negotiation requires TLS — so the two protocols split across <see cref="Port"/> (h2c gRPC)
    /// and this port (HTTP/1.1). With TLS enabled everything is served on <see cref="Port"/> and this is ignored.
    /// </summary>
    public int ManagementPort { get; init; } = 8080;

    /// <summary>
    /// Optional dedicated plaintext port that serves only the <see cref="HealthCheckEndpoint"/>, for orchestrator and
    /// load-balancer probes that cannot speak TLS. When <c>0</c> (the default) it is disabled. It is independent of
    /// TLS on <see cref="Port"/> — the data plane can stay on TLS while health checks answer over cleartext here.
    /// Bind it to the internal or cluster network only.
    /// </summary>
    public int HealthPort { get; init; }

    /// <summary>
    /// Gets the health check endpoint.
    /// </summary>
    public string HealthCheckEndpoint { get; init; } = "/health";

    /// <summary>
    /// Gets the <see cref="Events"/> configuration.
    /// </summary>
    public Events Events { get; init; } = new Events();

    /// <summary>
    /// Feature toggles for Chronicle.
    /// </summary>
    public Features Features { get; init; } = new Features();

    /// <summary>
    /// Gets or inits the storage configuration.
    /// </summary>
    public Storage Storage { get; init; } = new Storage();

    /// <summary>
    /// Gets or inits the compliance configuration.
    /// When <see cref="Encryption.Storage"/> is not set, the general <see cref="Storage"/> is used for compliance data.
    /// </summary>
    public Compliance Compliance { get; init; } = new Compliance();

    /// <summary>
    /// Gets the observers configuration.
    /// </summary>
    public Observers Observers { get; init; } = new Observers();

    /// <summary>
    /// Gets the clustering configuration.
    /// </summary>
    public Clustering Clustering { get; init; } = new Clustering();

    /// <summary>
    /// Gets the jobs configuration.
    /// </summary>
    public Jobs Jobs { get; init; } = new Jobs();

    /// <summary>
    /// Gets the read models configuration.
    /// </summary>
    public ReadModels ReadModels { get; init; } = new ReadModels();

    /// <summary>
    /// Gets the authentication configuration.
    /// </summary>
    public Authentication Authentication { get; init; } = new Authentication();

    /// <summary>
    /// Gets or inits the optional identity provider configuration.
    /// </summary>
    public IdentityProviderOptions? IdentityProvider { get; init; }

    /// <summary>
    /// Gets the encryption certificate configuration for Data Protection keys.
    /// When not configured, keys are auto-generated and stored in the database.
    /// </summary>
    public EncryptionCertificate EncryptionCertificate { get; init; } = new();

    /// <summary>
    /// Gets or inits the TLS configuration.
    /// </summary>
    public Tls Tls { get; init; } = new Tls();

    /// <summary>
    /// Gets or inits the bootstrap client configurations.
    /// Clients defined here are registered on startup with hashed secrets.
    /// </summary>
    public IEnumerable<ClientBootstrapConfig> Clients { get; init; } = [];

    /// <summary>
    /// Adds the Chronicle configuration.
    /// </summary>
    /// <param name="services">Service collection to add to.</param>
    /// <param name="configuration">Application configuration.</param>
    public static void AddConfiguration(IServiceCollection services, IConfiguration configuration)
    {
        // Load chronicle.json at the root level and bind it to the Cratis:Chronicle section
        var chronicleJsonConfig = new ConfigurationBuilder()
            .AddJsonFile("chronicle.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "Cratis__Chronicle__")
            .Build();

        // Add the chronicle.json values under the Cratis:Chronicle path
        foreach (var kvp in chronicleJsonConfig.AsEnumerable().Where(kvp => kvp.Value is not null))
        {
            configuration[$"{SectionPath}:{kvp.Key}"] = kvp.Value;
        }

        services
            .AddOptions<ChronicleOptions>()
            .BindConfiguration(SectionPath)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
