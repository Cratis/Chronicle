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
    public static ChronicleConnectionStringBuilder WithDevelopmentCredentials(this ChronicleConnectionStringBuilder builder
    {
        builder.Username = ChronicleConnectionString.DevelopmentClient;
        builder.Password = ChronicleConnectionString.DevelopmentClientSecret;
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
    /// Disables TLS for the connection.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    public static ChronicleConnectionStringBuilder WithTlsDisabled(this ChronicleConnectionStringBuilder builder)
    {
        builder.DisableTls = true;
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
    /// Enables TLS for the connection (default behavior).
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to configure.</param>
    /// <returns>The <see cref="ChronicleConnectionStringBuilder"/> for fluent configuration.</returns>
    public static ChronicleConnectionStringBuilder WithTlsEnabled(this ChronicleConnectionStringBuilder builder)
    {
        builder.DisableTls = false;
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
