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
    /// Gets the port for the REST API.
    /// </summary>
    public int ApiPort { get; init; } = 8080;

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
    /// Gets or inits the path to the certificate file for TLS.
    /// </summary>
    public string? CertificatePath { get; init; }

    /// <summary>
    /// Gets or inits the password for the certificate file.
    /// </summary>
    public string? CertificatePassword { get; init; }

    /// <summary>
    /// Gets or inits whether TLS is disabled. Default is false (TLS enabled).
    /// </summary>
    public bool DisableTls { get; init; }

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
