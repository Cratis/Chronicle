// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents a URL for connecting to the Chronicle.
/// </summary>
/// <remarks>
/// <![CDATA[
/// chronicle://<host>[:<port>]/?<options>
/// chronicle://username:password@<host>[:<port>]/?<options>
/// chronicle+srv://<host>[:<port>]/?<options>
/// chronicle://<host>[:<port>],<host>[:<port>],<host>[:<port>]/?<options>
///
/// // Regex patterns for valid Chronicle URLs:
/// // 1. chronicle://<host>[:<port>]/?<options>
/// const string SingleHostPattern = @"^chronicle:\/\/([a-zA-Z0-9\.-]+)(:\d+)?(\/\?.*)?$";
///
/// // 2. chronicle://username:password@<host>[:<port>]/?<options>
/// const string AuthPattern = @"^chronicle:\/\/([a-zA-Z0-9_\-\.]+):([^@]+)@([a-zA-Z0-9\.-]+)(:\d+)?(\/\?.*)?$";
///
/// // 3. chronicle+srv://<host>[:<port>]/?<options>
/// const string SrvPattern = @"^chronicle\+srv:\/\/([a-zA-Z0-9\.-]+)(:\d+)?(\/\?.*)?$";
///
/// // 4. chronicle://<host>[:<port>],<host>[:<port>],<host>[:<port>]/?<options>
/// const string MultiHostPattern = @"^chronicle:\/\/([a-zA-Z0-9\.-]+(:\d+)?)(,([a-zA-Z0-9\.-]+(:\d+)?))*\/(\?.*)?$";
/// ]]>
/// </remarks>
[TypeConverter(typeof(ChronicleConnectionStringConverter))]
public class ChronicleConnectionString
{
    /// <summary>
    /// The default <see cref="ChronicleConnectionString"/> pointing to localhost.
    /// </summary>
    public static readonly ChronicleConnectionString Default = new("chronicle://localhost:35000");

    /// <summary>
    /// The development <see cref="ChronicleConnectionString"/> pointing to localhost configured with the default dev credentials.
    /// </summary>
    public static readonly ChronicleConnectionString Development = new("chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000");

    readonly ChronicleConnectionStringBuilder _builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleConnectionString"/> class.
    /// </summary>
    /// <param name="connectionString">String representation of the connection string.</param>
    public ChronicleConnectionString(string connectionString)
    {
        _builder = new ChronicleConnectionStringBuilder(connectionString);
        ServerAddress = new ChronicleServerAddress(_builder.Host, _builder.Port);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleConnectionString"/> class.
    /// </summary>
    /// <param name="builder">The <see cref="ChronicleConnectionStringBuilder"/> to use.</param>
    ChronicleConnectionString(ChronicleConnectionStringBuilder builder)
    {
        _builder = builder;
        ServerAddress = new ChronicleServerAddress(_builder.Host, _builder.Port);
    }

    /// <summary>
    /// Gets the <see cref="ChronicleServerAddress"/> for the server.
    /// </summary>
    public ChronicleServerAddress ServerAddress { get; } = new("localhost", 35000);

    /// <summary>
    /// Gets the username for authentication, if specified. This maps to client id using `client_credentials` flow.
    /// </summary>
    public string? Username => _builder.Username;

    /// <summary>
    /// Gets the password for authentication, if specified. This maps to client secret using `client_credentials` flow.
    /// </summary>
    public string? Password => _builder.Password;

    /// <summary>
    /// Gets the authentication mode.
    /// </summary>
    public AuthenticationMode AuthenticationMode => _builder.AuthenticationMode;

    /// <summary>
    /// Gets the API key for ApiKey authentication, if specified.
    /// </summary>
    public string? ApiKey => _builder.ApiKey;

    /// <summary>
    /// Gets whether TLS is disabled.
    /// </summary>
    public bool DisableTls => _builder.DisableTls;

    /// <summary>
    /// Gets the path to the certificate file for TLS.
    /// </summary>
    public string? CertificatePath => _builder.CertificatePath;

    /// <summary>
    /// Gets the password for the certificate file.
    /// </summary>
    public string? CertificatePassword => _builder.CertificatePassword;

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ChronicleConnectionString"/>.
    /// </summary>
    /// <param name="connectionString">String connection string to convert from.</param>
    public static implicit operator ChronicleConnectionString(string connectionString) => new(connectionString);

    /// <summary>
    /// Creates a new <see cref="ChronicleConnectionString"/> with the specified username and password (Client Credentials).
    /// </summary>
    /// <param name="username">The username to set.</param>
    /// <param name="password">The password to set.</param>
    /// <returns>A new <see cref="ChronicleConnectionString"/> with the username and password set.</returns>
    public ChronicleConnectionString WithCredentials(string username, string password)
    {
        var newBuilder = new ChronicleConnectionStringBuilder(_builder.Build())
        {
            Username = username,
            Password = password
        };
        return new ChronicleConnectionString(newBuilder);
    }

    /// <summary>
    /// Creates a new <see cref="ChronicleConnectionString"/> with API key authentication.
    /// </summary>
    /// <param name="apiKey">The API key to use.</param>
    /// <returns>A new <see cref="ChronicleConnectionString"/> with API key authentication configured.</returns>
    public ChronicleConnectionString WithApiKey(string apiKey)
    {
        var newBuilder = new ChronicleConnectionStringBuilder(_builder.Build())
        {
            ApiKey = apiKey
        };
        return new ChronicleConnectionString(newBuilder);
    }

    /// <inheritdoc/>
    public override string ToString() => _builder.Build();
}
