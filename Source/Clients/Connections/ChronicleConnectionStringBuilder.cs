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
/// chronicle://[username:password@]host[:port][/?options].
/// </remarks>
[SuppressMessage("Design", "CA1010:Collections should implement generic interface", Justification = "Inheriting from DbConnectionStringBuilder")]
public class ChronicleConnectionStringBuilder : DbConnectionStringBuilder
{
    const string HostKey = "Host";
    const string PortKey = "Port";
    const string UsernameKey = "Username";
    const string PasswordKey = "Password";
    const string SchemeKey = "Scheme";
    const string AuthKey = "auth";
    const string ApiKeyKey = "key";
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
    /// Gets or sets the host.
    /// </summary>
    public string Host
    {
        get => ContainsKey(HostKey) ? (string)this[HostKey] : "localhost";
        set => this[HostKey] = value;
    }

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    public int Port
    {
        get => ContainsKey(PortKey) ? Convert.ToInt32(this[PortKey]) : DefaultPort;
        set => this[PortKey] = value;
    }

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
    /// Gets or sets the authentication mode.
    /// </summary>
    public AuthenticationMode AuthenticationMode
    {
        get
        {
            if (!ContainsKey(AuthKey))
            {
                // Infer from username/password if present
                if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password))
                {
                    return AuthenticationMode.InternalUserPassword;
                }
                return AuthenticationMode.None;
            }

            var authValue = (string)this[AuthKey];
            return Enum.TryParse<AuthenticationMode>(authValue, true, out var mode) ? mode : AuthenticationMode.None;
        }
        set
        {
            if (value == AuthenticationMode.None)
            {
                Remove(AuthKey);
            }
            else
            {
                this[AuthKey] = value.ToString();
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
    /// Builds a Chronicle URL string from the current settings.
    /// </summary>
    /// <returns>The Chronicle URL string.</returns>
    [SuppressMessage("Design", "CA1055:Uri return values should not be strings", Justification = "Returning a Chronicle URL string format")]
    public string BuildChronicleUrl()
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

        url += Host;

        if (Port != DefaultPort)
        {
            url += $":{Port}";
        }

        // Add query parameters if needed
        var queryParams = new List<string>();

        if (ContainsKey(AuthKey))
        {
            queryParams.Add($"auth={Uri.EscapeDataString((string)this[AuthKey])}");
        }

        if (ContainsKey(ApiKeyKey))
        {
            queryParams.Add($"key={Uri.EscapeDataString((string)this[ApiKeyKey])}");
        }

        // Add any other query parameters that aren't our special keys
        foreach (var key in Keys)
        {
            var keyStr = key.ToString();
            if (keyStr != null &&
                keyStr != HostKey &&
                keyStr != PortKey &&
                keyStr != UsernameKey &&
                keyStr != PasswordKey &&
                keyStr != SchemeKey &&
                keyStr != AuthKey &&
                keyStr != ApiKeyKey)
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
            ParseChronicleUrl(connectionString);
        }
        else
        {
            // Fall back to standard key=value parsing
            ConnectionString = connectionString;
        }
    }

    void ParseChronicleUrl(string url)
    {
        var uri = new Uri(url);

        // Extract scheme
        Scheme = uri.Scheme;

        // Extract host
        Host = uri.Host;

        // Extract port
        Port = uri.Port == -1 ? DefaultPort : uri.Port;

        // Extract username and password from UserInfo
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':');
            Username = Uri.UnescapeDataString(parts[0]);
            if (parts.Length > 1)
            {
                Password = Uri.UnescapeDataString(parts[1]);
            }
        }

        // Parse query string parameters if any
        if (!string.IsNullOrEmpty(uri.Query))
        {
            foreach (var pair in uri.Query.TrimStart('?').Split('&'))
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
