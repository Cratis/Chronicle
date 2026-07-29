// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.ExternalServices;

/// <summary>
/// Represents the MongoDB representation of a database external service endpoint configuration.
/// </summary>
public class DatabaseEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the database host.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database port. A value of 0 means the provider default is used.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username used to connect.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password used to connect.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional provider-specific options.
    /// </summary>
    public IDictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
}
