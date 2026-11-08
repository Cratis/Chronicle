// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Diagnostics;
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
[DebuggerDisplay("{BuildRedacted(),nq}")]
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
    const string AuthKey = "auth";
    const string NoAuthenticationValue = "none";
    const string SkipTlsValidationKey = "skipTlsValidation";
    const string DisableTlsKey = "disableTls";
    const string SkipCompatibilityCheckKey = "skipCompatibilityCheck";
    const string SkipKeepAliveKey = "skipKeepAlive";
    const string ServerlessKey = "serverless";
    const string LoadBalancerKey = "loadBalancer";
    const string SrvNameServerKey = "srvNameServer";
    const string CertificatePathKey = "certificatePath";
    const string CertificatePasswordKey = "certificatePassword";
    const string CaCertificatePathKey = "caCertificatePath";
    const string SrvScheme = "chronicle+srv";
    const int DefaultPort = 35000;

    static readonly HashSet<string> _keysThatMayHoldAtSign = new([ApiKeyKey, CertificatePathKey, CertificatePasswordKey, CaCertificatePathKey, LoadBalancerKey, SrvNameServerKey], StringComparer.OrdinalIgnoreCase);

    static readonly HashSet<string> _keysWithDedicatedRendering =
    [
        HostKey,
        PortKey,
        ServersKey,
        UsernameKey,
        PasswordKey,
        SchemeKey,
        ApiKeyKey,
        SkipTlsValidationKey,
        DisableTlsKey,
        SkipCompatibilityCheckKey,
        SkipKeepAliveKey,
        ServerlessKey,
        LoadBalancerKey,
        SrvNameServerKey,
        CertificatePathKey,
        CertificatePasswordKey,
        CaCertificatePathKey
    ];

    readonly bool _hasUncertainDiagnosticInput = true;

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
        _hasUncertainDiagnosticInput = !ConnectionStringDiagnostics.HasUnambiguousStructure(connectionString);
        ParseConnectionString(connectionString);
    }

    /// <summary>
    /// Initializes a diagnostic-preserving copy without changing raw serialization.
    /// </summary>
    /// <param name="source">The builder to copy.</param>
    internal ChronicleConnectionStringBuilder(ChronicleConnectionStringBuilder source)
        : this(source.Build())
    {
        _hasUncertainDiagnosticInput |= source._hasUncertainDiagnosticInput;
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
        get => ContainsKey(PortKey) ? ConvertOption(PortKey, Convert.ToInt32) : DefaultPort;
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

            // Asking for no authentication is deliberate and beats anything else the connection string carries -
            // credentials left in place by a shared configuration must not quietly turn the exchange back on.
            if (NoAuthentication)
            {
                return AuthenticationMode.None;
            }

            if (hasClientCredentials && hasApiKey)
            {
                throw new AmbiguousAuthenticationMode();
            }

            // Without TLS the development credentials have nowhere to go: Chronicle serves its token endpoint over
            // TLS only. A cleartext connection with no credentials therefore authenticates with nothing.
            if (hasNoAuthentication && DisableTls)
            {
                return AuthenticationMode.None;
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
    /// Gets or sets whether the client connects without presenting any credentials, expressed as
    /// <c language="csharp">auth=none</c> in the connection string.
    /// </summary>
    /// <remarks>
    /// Only works against a server running with authentication turned off
    /// (<c language="csharp">Cratis:Chronicle:Authentication:Enabled=false</c>). Note that omitting credentials entirely does
    /// <em>not</em> mean this - a connection string with no credentials still performs a client-credentials
    /// exchange using the development credentials, which is what it has always done.
    /// </remarks>
    public bool NoAuthentication
    {
        get => ContainsKey(AuthKey) && string.Equals((string)this[AuthKey], NoAuthenticationValue, StringComparison.OrdinalIgnoreCase);
        set
        {
            if (value)
            {
                this[AuthKey] = NoAuthenticationValue;
            }
            else
            {
                Remove(AuthKey);
            }
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
    /// Gets or sets whether to skip TLS certificate validation when connecting.
    /// </summary>
    /// <remarks>
    /// Defaults to true in this release; the next major release will require validation by default.
    /// Set <c language="csharp">skipTlsValidation=false</c> explicitly to require certificate validation.
    /// An explicit false in either the connection string or TLS options requires validation.
    /// Use true only as a development opt-out. This option has no effect when TLS is disabled.
    /// </remarks>
    public bool SkipTlsValidation
    {
        get => !ContainsKey(SkipTlsValidationKey) || ConvertOption(SkipTlsValidationKey, Convert.ToBoolean);
        set => this[SkipTlsValidationKey] = value;
    }

    /// <summary>
    /// Gets or sets whether the client connects without TLS, expressed as <c language="csharp">disableTls=true</c> in the
    /// connection string.
    /// </summary>
    /// <remarks>
    /// For a Chronicle server running with <c language="csharp">tls.enabled=false</c>, typically reached through a
    /// port-forward or behind a proxy that forwards cleartext HTTP/2 (h2c). The client then speaks HTTP/2 without TLS.
    /// Chronicle never serves its token endpoint without TLS, so client credentials cannot be exchanged on such a
    /// connection: use <c language="csharp">auth=none</c> against a server with authentication turned off, or
    /// <c language="csharp">apiKey=</c> with a token from the server's external authority. A connection string with no
    /// credentials at all connects without authentication.
    /// </remarks>
    public bool DisableTls
    {
        get => ContainsKey(DisableTlsKey) && ConvertOption(DisableTlsKey, Convert.ToBoolean);
        set
        {
            if (value)
            {
                this[DisableTlsKey] = true;
            }
            else
            {
                Remove(DisableTlsKey);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to skip the server compatibility check on connect.
    /// </summary>
    /// <remarks>
    /// The client asks the server, on every connect, whether it still serves the contract this client was
    /// built against, and refuses to connect when the server says no (see
    /// <c language="csharp">Source/Kernel/Compatibility</c>). That comparison runs on the server, so it is
    /// only as current as the kernel build answering it — a kernel that has not been redeployed since a
    /// wire-compatibility defect was fixed still computes the old, incorrect verdict, and no client-side
    /// change can make it compute a different one. Set <c language="csharp">skipCompatibilityCheck=true</c>
    /// as a deliberate, connection-string-visible escape hatch for exactly that situation — a known-safe
    /// skew against a kernel that cannot be redeployed to fix the check itself. It is not a general-purpose
    /// override: turning it on hides every other incompatibility too, including a genuine one.
    /// </remarks>
    public bool SkipCompatibilityCheck
    {
        get => ContainsKey(SkipCompatibilityCheckKey) && ConvertOption(SkipCompatibilityCheckKey, Convert.ToBoolean);
        set => this[SkipCompatibilityCheckKey] = value;
    }

    /// <summary>
    /// Gets or sets whether to skip the keep-alive connection on connect.
    /// </summary>
    /// <remarks>
    /// Skipping keep-alive avoids the long-lived keep-alive stream and its watchdog, which suits short-lived
    /// processes such as command line tools, functions and jobs. A client that skips keep-alive cannot host
    /// reactors or reducers, as the kernel has no live connection to deliver events on.
    /// </remarks>
    public bool SkipKeepAlive
    {
        get => ContainsKey(SkipKeepAliveKey) && ConvertOption(SkipKeepAliveKey, Convert.ToBoolean);
        set => this[SkipKeepAliveKey] = value;
    }

    /// <summary>
    /// Gets or sets whether the .NET client runs in serverless mode.
    /// </summary>
    /// <remarks>
    /// Serverless mode implies skipping the keep-alive connection. It is equal to setting
    /// <c language="json">Cratis:Chronicle:Serverless:Mode</c> to <c language="csharp">Serverless</c> in the .NET client. Other clients ignore the key.
    /// </remarks>
    [Experimental("CHRX0001", UrlFormat = "https://www.cratis.io/docs/chronicle/clients/dotnet/serverless-mode")]
    public bool Serverless
    {
        get => ContainsKey(ServerlessKey) && Convert.ToBoolean(this[ServerlessKey]);
        set => this[ServerlessKey] = value;
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
    /// Gets or sets the path to a file with the certificate authorities (CA) the server certificate must chain to,
    /// expressed as <c language="csharp">caCertificatePath=</c> in the connection string.
    /// </summary>
    /// <remarks>
    /// The file holds one or more CA certificates in PEM or PKCS#7 format. The certificate authorities are used only
    /// when certificate validation is enabled (<c language="csharp">skipTlsValidation=false</c>); the server certificate
    /// is then accepted only when its chain builds to one of them. This option has no effect when TLS is disabled.
    /// </remarks>
    public string? CaCertificatePath
    {
        get => ContainsKey(CaCertificatePathKey) ? (string)this[CaCertificatePathKey] : null;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Remove(CaCertificatePathKey);
            }
            else
            {
                this[CaCertificatePathKey] = value;
            }
        }
    }

    /// <summary>
    /// Gets whether certificate validation was explicitly configured.
    /// </summary>
    internal bool HasExplicitSkipTlsValidation => ContainsKey(SkipTlsValidationKey);

    /// <summary>
    /// Builds a Chronicle connection string from the current settings.
    /// </summary>
    /// <returns>The Chronicle connection string.</returns>
    /// <remarks>
    /// The result carries every credential in clear text and must never be logged or included in an
    /// error message. Use <see cref="BuildRedacted"/> for anything that leaves the process.
    /// </remarks>
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

        if (HasExplicitSkipTlsValidation)
        {
            queryParams.Add($"skipTlsValidation={SkipTlsValidation.ToString().ToLowerInvariant()}");
        }

        if (DisableTls)
        {
            queryParams.Add("disableTls=true");
        }

        if (SkipCompatibilityCheck)
        {
            queryParams.Add("skipCompatibilityCheck=true");
        }

        AddSkipKeepAlive(queryParams);
        AddServerless(queryParams);

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

        if (ContainsKey(CaCertificatePathKey))
        {
            queryParams.Add($"caCertificatePath={Uri.EscapeDataString((string)this[CaCertificatePathKey])}");
        }

        // Add any other query parameters that aren't our special keys
        foreach (var key in Keys)
        {
            var keyStr = key.ToString();
            if (keyStr != null && !_keysWithDedicatedRendering.Contains(keyStr))
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

    /// <summary>
    /// Builds a Chronicle connection string from the current settings with every credential replaced by a mask.
    /// </summary>
    /// <returns>The Chronicle connection string, safe to log.</returns>
    /// <remarks>
    /// Only structurally attributable endpoints and explicitly recognized settings are projected.
    /// Credentials are masked; ambiguous input, paths and arbitrary options produce an opaque
    /// <c language="csharp">REDACTED</c>. The result is not a usable connection string.
    /// </remarks>
    [SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "Returning a Chronicle URL string format")]
    public string BuildRedacted() => ConnectionStringDiagnostics.Build(this, _hasUncertainDiagnosticInput);

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

    static void ThrowIfUserInfoIsNotEncoded(string rest, int pathIndex)
    {
        if (pathIndex == -1)
        {
            return;
        }

        // Nothing after the authority may carry an at sign, except the value of a setting that is known to hold one.
        // An at sign anywhere else means the user information contained a separator that was not percent-encoded.
        var tail = rest[pathIndex..];
        var queryIndex = tail.IndexOf('?', StringComparison.Ordinal);
        var path = queryIndex == -1 ? tail : tail[..queryIndex];
        var query = queryIndex == -1 ? string.Empty : tail[(queryIndex + 1)..];

        if (path.Contains('@', StringComparison.Ordinal) || query.Split('&').Any(IsPairWithMisplacedAtSign))
        {
            throw new InvalidUserInfo();
        }
    }

    static bool IsPairWithMisplacedAtSign(string pair)
    {
        if (!pair.Contains('@', StringComparison.Ordinal))
        {
            return false;
        }

        var equalsIndex = pair.IndexOf('=', StringComparison.Ordinal);
        if (equalsIndex == -1 || pair.IndexOf('@', StringComparison.Ordinal) < equalsIndex)
        {
            return true;
        }

        var key = Uri.UnescapeDataString(pair[..equalsIndex]);
        return !_keysThatMayHoldAtSign.Contains(key);
    }

    /// <summary>
    /// Converts an option, naming only the option key when it is rejected instead of quoting configuration text.
    /// </summary>
    /// <typeparam name="T">The option type.</typeparam>
    /// <param name="key">The option key.</param>
    /// <param name="convert">The conversion to apply.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="FormatException">Thrown when the value is not in a valid format.</exception>
    /// <exception cref="OverflowException">Thrown when the value is outside the range of the option type.</exception>
    T ConvertOption<T>(string key, Func<object, T> convert)
    {
        try
        {
            return convert(this[key]);
        }
        catch (OverflowException)
        {
            throw new OverflowException($"The value of the '{key}' option is invalid.");
        }
        catch (FormatException)
        {
            throw new FormatException($"The value of the '{key}' option is invalid.");
        }
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

    void AddSkipKeepAlive(List<string> queryParams)
    {
        if (SkipKeepAlive)
        {
            queryParams.Add("skipKeepAlive=true");
        }
    }

    void AddServerless(List<string> queryParams)
    {
#pragma warning disable CHRX0001 // The serverless switch is experimental; the builder serialises it internally.
        if (Serverless)
#pragma warning restore CHRX0001
        {
            queryParams.Add("serverless=true");
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

        ThrowIfUserInfoIsNotEncoded(rest, pathIndex);

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
