// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Represents the configuration of an <see cref="IPEndPoint"/>.
/// </summary>
public class EndPoint
{
    /// <summary>
    /// Gets the address to use, this can either be an IP address or a hostname.
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// Gets the port.
    /// </summary>
    public int Port { get; init; } = 30000;
}
