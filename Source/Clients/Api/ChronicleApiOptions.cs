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
}
