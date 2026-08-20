// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Extension methods for <see cref="ChronicleConnectionStringBuilder"/>.
/// </summary>
public static class ChronicleConnectionStringBuilderExtensions
{
    /// <summary>
    /// Sets the host for the connection.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <param name="host">The host name or IP address.</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    public static ChronicleConnectionStringBuilder WithHost(this ChronicleConnectionStringBuilder builder, string host)
    {
        builder.Host = host;
        return builder;
    }

    /// <summary>
    /// Sets the port for the connection.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <param name="port">The port number.</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    public static ChronicleConnectionStringBuilder WithPort(this ChronicleConnectionStringBuilder builder, int port)
    {
        builder.Port = port;
        return builder;
    }

    /// <summary>
    /// Sets the server addresses for the connection, supporting multiple servers for load balancing.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <param name="serverAddresses">The <see cref="ChronicleServerAddress"/> entries to connect to.</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    public static ChronicleConnectionStringBuilder WithServerAddresses(this ChronicleConnectionStringBuilder builder, params ChronicleServerAddress[] serverAddresses)
    {
        builder.ServerAddresses = serverAddresses;
        return builder;
    }

    /// <summary>
    /// Sets the load balancer strategy to use when multiple servers are available.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <param name="loadBalancer">The name of the load balancer strategy.</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    public static ChronicleConnectionStringBuilder WithLoadBalancer(this ChronicleConnectionStringBuilder builder, string loadBalancer)
    {
        builder.LoadBalancer = loadBalancer;
        return builder;
    }

    /// <summary>
    /// Sets the username and password for client credentials authentication.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <param name="username">The username (client ID).</param>
    /// <param name="password">The password (client secret).</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    public static ChronicleConnectionStringBuilder WithCredentials(this ChronicleConnectionStringBuilder builder, string username, string password)
    {
        builder.Username = username;
        builder.Password = password;
        return builder;
    }

    /// <summary>
    /// Sets the username and password for client credentials authentication.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    public static ChronicleConnectionStringBuilder WithDevelopmentCredentials(this ChronicleConnectionStringBuilder builder)
    {
        builder.Username = ChronicleConnectionString.DevelopmentClient;
        builder.Password = ChronicleConnectionString.DevelopmentClientSecret;
        return builder;
    }

    /// <summary>
    /// Connects without presenting any credentials.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    /// <remarks>
    /// Only works against a server running with authentication turned off
    /// (<c>Cratis:Chronicle:Authentication:Enabled=false</c>) - typically a Chronicle embedded in the same
    /// container or process as its client. It skips the token exchange entirely, which is what makes a cold
    /// start of such an instance fast.
    /// </remarks>
    public static ChronicleConnectionStringBuilder WithoutAuthentication(this ChronicleConnectionStringBuilder builder)
    {
        builder.NoAuthentication = true;
        return builder;
    }

    /// <summary>
    /// Sets the API key for API key authentication.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <param name="apiKey">The API key to use.</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    public static ChronicleConnectionStringBuilder WithApiKey(this ChronicleConnectionStringBuilder builder, string apiKey)
    {
        builder.ApiKey = apiKey;
        return builder;
    }

    /// <summary>
    /// Skips TLS certificate validation for the connection.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    /// <remarks>
    /// The client still connects over TLS but accepts any server certificate, including self-signed ones.
    /// Only use this for a trusted server on a trusted network.
    /// </remarks>
    public static ChronicleConnectionStringBuilder WithTlsValidationSkipped(this ChronicleConnectionStringBuilder builder)
    {
        builder.SkipTlsValidation = true;
        return builder;
    }

    /// <summary>
    /// Configures the TLS certificate for the connection.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <param name="certificatePath">The path to the certificate file.</param>
    /// <param name="certificatePassword">The password for the certificate file.</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    public static ChronicleConnectionStringBuilder WithCertificate(this ChronicleConnectionStringBuilder builder, string certificatePath, string certificatePassword)
    {
        builder.CertificatePath = certificatePath;
        builder.CertificatePassword = certificatePassword;
        return builder;
    }

    /// <summary>
    /// Sets the scheme for the connection.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <param name="scheme">The scheme (e.g., "chronicle" or "chronicle+srv").</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    public static ChronicleConnectionStringBuilder WithScheme(this ChronicleConnectionStringBuilder builder, string scheme)
    {
        builder.Scheme = scheme;
        return builder;
    }

    /// <summary>
    /// Builds the Chronicle connection URL from the builder.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to build from.</param>
    /// <returns>The built connection string.</returns>
    public static string Build(this ChronicleConnectionStringBuilder builder) => builder.Build();

    /// <summary>
    /// Converts the builder to a <see cref="ChronicleConnectionString"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to convert.</param>
    /// <returns>A new <see cref="ChronicleConnectionString"/> instance.</returns>
    public static ChronicleConnectionString ToConnectionString(this ChronicleConnectionStringBuilder builder) => new(builder.Build());
}
