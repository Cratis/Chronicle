// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Api;

/// <summary>
/// Represents the Chronicle options.
/// </summary>
public class ChronicleApiOptions
{
    /// <summary>
    /// Gets the port for the REST API.
    /// </summary>
    public int ApiPort { get; init; } = 8080;

    /// <summary>
    /// Gets the <see cref="ChronicleUrl"/> to use.
    /// </summary>
    public ChronicleUrl ChronicleUrl { get; init; } = ChronicleUrl.Default;

    /// <summary>
    /// Gets the timeout for connecting in seconds.
    /// </summary>
    public int ConnectTimeout { get; init; } = 5;
}
