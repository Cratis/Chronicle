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
    /// Port to listen on for gRPC.
    /// </summary>
    public int Port { get; init; } = 35000;

    /// <summary>
    /// Gets the port for the Management API and well-known certificate endpoint.
    /// </summary>
    public int ManagementPort { get; init; } = 8080;

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
    /// Gets the observers configuration.
    /// </summary>
    public Observers Observers { get; init; } = new Observers();

    /// <summary>
    /// Gets the jobs configuration.
    /// </summary>
    public Jobs Jobs { get; init; } = new Jobs();

    /// <summary>
    /// Gets the authentication configuration.
    /// </summary>
    public Authentication Authentication { get; init; } = new Authentication();

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
