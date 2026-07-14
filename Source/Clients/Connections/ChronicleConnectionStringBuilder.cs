// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents a connection string builder for Chronicle URLs.
/// </summary>
/// <remarks>
/// Supports parsing and building Chronicle connection strings in the format:
/// chronicle://[username:password@]host[:port][,host[:port],...][/?options] and
/// chronicle+srv://host[/?options] for DNS SRV record lookup.
/// </remarks>
#pragma warning disable CA1010 // Generic interface should also be implemented
public class ChronicleConnectionStringBuilder : DbConnectionStringBuilder
#pragma warning restore CA1010 // Generic interface should also be implemented
{
    const string HostKey = "Host";
    const string PortKey = "Port";
    const string ServersKey = "Servers";
    const string UsernameKey = "Username";
    const string PasswordKey = "Password";
    const string SchemeKey = "Scheme";
    const string ApiKeyKey = "apiKey";
    const string DisableTlsKey = "disableTls";
    const string LoadBalancerKey = "loadBalancer";
    const string SrvNameServerKey = "srvNameServer";
    const string CertificatePathKey = "certificatePath";
    const string CertificatePasswordKey = "certificatePassword";
    const string SrvScheme = "chronicle+srv";
    const int DefaultPort = 35000;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleConnectionStringBuilder"/> class.
    /// </summary>
    public ChronicleConnectionStringBuilder()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChronicleConnectionStringBuilder"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    public ChronicleConnectionStringBuilder(string connectionString)
    {
        ParseConnectionString(connectionString);
    }

    /// <summary>
    /// Gets or sets the host. When multiple servers are configured, this is the first server's host.
    /// Setting it resets the configuration to a single server.
    /// </summary>
    public string Host
    {
        get => ContainsKey(HostKey) ? (string)this[HostKey] : "localhost";
        set
        {
            Remove(ServersKey);
            this[HostKey] = value;
        }
    }

    /// <summary>
    /// Gets or sets the port. When multiple servers are configured, this is the first server's port.
    /// Setting it resets the configuration to a single server.
    /// </summary>
    public int Port
    {
        get => ContainsKey(PortKey) ? Convert.ToInt32(this[PortKey]) : DefaultPort;
        set
        {
            Remove(ServersKey);
            this[PortKey] = value;
        }
    }

    /// <summary>
    /// Gets or sets the server addresses. Supports multiple servers for load balancing.
    /// </summary>
    /// <exception cref="MissingServerAddress">Thrown when setting an empty collection of addresses.</exception>
    public IReadOnlyList<ChronicleServerAddress> ServerAddresses
    {
        get => ContainsKey(ServersKey)
            ? ParseServerAddresses((string)this[ServersKey])
            : [new ChronicleServerAddress(Host, Port)];
        set
        {
            if (value.Count == 0)
            {
                throw new MissingServerAddress();
            }

            Host = value[0].Host;
            Port = value[0].Port;
            if (value.Count > 1)
            {
                this[ServersKey] = string.Join(',', value.Select(address => address.ToString()));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the connection string uses the DNS SRV lookup scheme (chronicle+srv).
    /// </summary>
    public bool IsSrv => Scheme.Equals(SrvScheme, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    public string? Username
    {
        get => ContainsKey(UsernameKey) ? (string)this[UsernameKey] : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Remove(UsernameKey);
            }
            else
            {
                this[UsernameKey] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    public string? Password
    {
        get => ContainsKey(PasswordKey) ? (string)this[PasswordKey] : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Remove(PasswordKey);
            }
            else
            {
                this[PasswordKey] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the scheme (e.g., "chronicle" or "chronicle+srv").
    /// </summary>
    public string Scheme
    {
        get => ContainsKey(SchemeKey) ? (string)this[SchemeKey] : "chronicle";
        set => this[SchemeKey] = value;
    }

    /// <summary>
    /// Gets the authentication mode based on the configured credentials.
    /// </summary>
    /// <exception cref="AmbiguousAuthenticationMode">Thrown when both client credentials and API key are specified.</exception>
    /// <exception cref="MissingAuthentication">Thrown when no authentication method is specified.</exception>
    public AuthenticationMode AuthenticationMode
    {
        get
        {
            var hasUsername = !string.IsNullOrEmpty(Username);
            var hasPassword = !string.IsNullOrEmpty(Password);
            var hasClientCredentials = hasUsername && hasPassword;
            var hasApiKey = !string.IsNullOrEmpty(ApiKey);
            var hasNoAuthentication = !hasUsername && !hasPassword && !hasApiKey;

            if (hasClientCredentials && hasApiKey)
            {
                throw new AmbiguousAuthenticationMode();
            }

            if (hasClientCredentials || hasNoAuthentication)
            {
                return AuthenticationMode.ClientCredentials;
            }

            if (hasApiKey)
            {
                return AuthenticationMode.ApiKey;
            }

            throw new MissingAuthentication();
        }
    }

    /// <summary>
    /// Gets or sets the API key for ApiKey authentication.
    /// </summary>
    public string? ApiKey
    {
        get => ContainsKey(ApiKeyKey) ? (string)this[ApiKeyKey] : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Remove(ApiKeyKey);
            }
            else
            {
                this[ApiKeyKey] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether TLS is disabled.
    /// </summary>
    public bool DisableTls
    {
        get => ContainsKey(DisableTlsKey) && Convert.ToBoolean(this[DisableTlsKey]);
        set => this[DisableTlsKey] = value;
    }

    /// <summary>
    /// Gets or sets the name of the load balancer strategy to use when multiple servers are available.
    /// </summary>
    public string? LoadBalancer
    {
        get => ContainsKey(LoadBalancerKey) ? (string)this[LoadBalancerKey] : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Remove(LoadBalancerKey);
            }
            else
            {
                this[LoadBalancerKey] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the DNS name server (host[:port], port defaults to 53) used for chronicle+srv
    /// lookups. When not set, the system's configured name servers are used.
    /// </summary>
    public string? SrvNameServer
    {
        get => ContainsKey(SrvNameServerKey) ? (string)this[SrvNameServerKey] : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Remove(SrvNameServerKey);
            }
            else
            {
                this[SrvNameServerKey] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the path to the certificate file for TLS.
    /// </summary>
    public string? CertificatePath
    {
        get => ContainsKey(CertificatePathKey) ? (string)this[CertificatePathKey] : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Remove(CertificatePathKey);
            }
            else
            {
                this[CertificatePathKey] = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the password for the certificate file.
    /// </summary>
    public string? CertificatePassword
    {
        get => ContainsKey(CertificatePasswordKey) ? (string)this[CertificatePasswordKey] : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Remove(CertificatePasswordKey);
            }
            else
            {
                this[CertificatePasswordKey] = value;
            }
        }
    }

    /// <summary>
    /// Builds a Chronicle connection string from the current settings.
    /// </summary>
    /// <returns>The Chronicle connection string.</returns>
    [SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "Returning a Chronicle URL string format")]
    public string Build()
    {
        var url = $"{Scheme}://";

        if (!string.IsNullOrEmpty(Username))
        {
            url += Username;
            if (!string.IsNullOrEmpty(Password))
            {
                url += $":{Password}";
            }
            url += "@";
        }

        url += ContainsKey(ServersKey)
            ? string.Join(',', ServerAddresses.Select(address => address.ToString()))
            : $"{Host}:{Port}";

        // Add query parameters if needed
        var queryParams = new List<string>();

        if (ContainsKey(ApiKeyKey))
        {
            queryParams.Add($"apiKey={Uri.EscapeDataString((string)this[ApiKeyKey])}");
        }

        if (DisableTls)
        {
            queryParams.Add("disableTls=true");
        }

        if (ContainsKey(LoadBalancerKey))
        {
            queryParams.Add($"loadBalancer={Uri.EscapeDataString((string)this[LoadBalancerKey])}");
        }

        if (ContainsKey(SrvNameServerKey))
        {
            queryParams.Add($"srvNameServer={Uri.EscapeDataString((string)this[SrvNameServerKey])}");
        }

        if (ContainsKey(CertificatePathKey))
        {
            queryParams.Add($"certificatePath={Uri.EscapeDataString((string)this[CertificatePathKey])}");
        }

        if (ContainsKey(CertificatePasswordKey))
        {
            queryParams.Add($"certificatePassword={Uri.EscapeDataString((string)this[CertificatePasswordKey])}");
        }

        // Add any other query parameters that aren't our special keys
        foreach (var key in Keys)
        {
            var keyStr = key.ToString();
            if (keyStr != null &&
                keyStr != HostKey &&
                keyStr != PortKey &&
                keyStr != ServersKey &&
                keyStr != UsernameKey &&
                keyStr != PasswordKey &&
                keyStr != SchemeKey &&
                keyStr != ApiKeyKey &&
                keyStr != DisableTlsKey &&
                keyStr != LoadBalancerKey &&
                keyStr != SrvNameServerKey &&
                keyStr != CertificatePathKey &&
                keyStr != CertificatePasswordKey)
            {
                queryParams.Add($"{Uri.EscapeDataString(keyStr)}={Uri.EscapeDataString(this[keyStr]?.ToString() ?? string.Empty)}");
            }
        }

        if (queryParams.Count > 0)
        {
            url += '?' + string.Join('&', queryParams);
        }

        return url;
    }

    static ChronicleServerAddress[] ParseServerAddresses(string authority)
    {
        var addresses = authority
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseServerAddress)
            .ToArray();

        return addresses.Length == 0 ? throw new MissingServerAddress() : addresses;
    }

    static ChronicleServerAddress ParseServerAddress(string entry)
    {
        string host;
        string portPart;

        if (entry.StartsWith('['))
        {
            // IPv6 literal, e.g. [::1]:35000
            var closingBracketIndex = entry.IndexOf(']');
            if (closingBracketIndex == -1)
            {
                throw new InvalidServerAddress(entry);
            }

            host = entry[1..closingBracketIndex];
            portPart = entry[(closingBracketIndex + 1)..].TrimStart(':');
        }
        else
        {
            var colonIndex = entry.LastIndexOf(':');
            host = colonIndex == -1 ? entry : entry[..colonIndex];
            portPart = colonIndex == -1 ? string.Empty : entry[(colonIndex + 1)..];
        }

        if (host.Length == 0)
        {
            throw new InvalidServerAddress(entry);
        }

        if (portPart.Length == 0)
        {
            return new ChronicleServerAddress(host, DefaultPort);
        }

        return int.TryParse(portPart, out var port)
            ? new ChronicleServerAddress(host, port)
            : throw new InvalidServerAddress(entry);
    }

    void ParseConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        // Check if it's a Chronicle URL format
        if (connectionString.StartsWith("chronicle://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("chronicle+srv://", StringComparison.OrdinalIgnoreCase))
        {
            Parse(connectionString);
        }
        else
        {
            // Fall back to standard key=value parsing
            ConnectionString = connectionString;
        }
    }

    void Parse(string url)
    {
        var schemeSeparatorIndex = url.IndexOf("://", StringComparison.Ordinal);
        Scheme = url[..schemeSeparatorIndex];
        var rest = url[(schemeSeparatorIndex + 3)..];

        // Separate the authority (userinfo + hosts) from path and query
        var pathIndex = rest.IndexOfAny(['/', '?']);
        var authority = pathIndex == -1 ? rest : rest[..pathIndex];
        var query = string.Empty;
        if (pathIndex != -1)
        {
            var queryIndex = rest.IndexOf('?', pathIndex);
            query = queryIndex == -1 ? string.Empty : rest[(queryIndex + 1)..];
        }

        // Extract username and password from the userinfo part
        var userInfoIndex = authority.LastIndexOf('@');
        if (userInfoIndex != -1)
        {
            var userInfo = authority[..userInfoIndex];
            authority = authority[(userInfoIndex + 1)..];
            var parts = userInfo.Split(':');
            Username = Uri.UnescapeDataString(parts[0]);
            if (parts.Length > 1)
            {
                Password = Uri.UnescapeDataString(parts[1]);
            }
        }

        ServerAddresses = ParseServerAddresses(authority);

        // Parse query string parameters if any
        if (!string.IsNullOrEmpty(query))
        {
            foreach (var pair in query.Split('&'))
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    this[Uri.UnescapeDataString(keyValue[0])] = Uri.UnescapeDataString(keyValue[1]);
                }
            }
        }
    }
}
