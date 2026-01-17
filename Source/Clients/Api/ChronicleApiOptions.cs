// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api;

/// <summary>
/// Represents the Chronicle options.
/// </summary>
public class ChronicleApiOptions
{
    /// <summary>
    /// Gets the port for the Management API.
    /// </summary>
    public int ManagementPort { get; init; } = 8080;
}
