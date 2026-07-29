// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ExternalServices;

/// <summary>
/// Represents the configuration for a database external service endpoint.
/// </summary>
[ProtoContract]
public class DatabaseEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the database host.
    /// </summary>
    [ProtoMember(1)]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database port. A value of 0 means the provider default is used.
    /// </summary>
    [ProtoMember(2)]
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    [ProtoMember(3)]
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username used to connect.
    /// </summary>
    [ProtoMember(4)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password used to connect.
    /// </summary>
    [ProtoMember(5)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional provider-specific options.
    /// </summary>
    [ProtoMember(6, IsRequired = true)]
    public IDictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
}
