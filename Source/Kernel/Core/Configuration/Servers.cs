// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for server instance diagnostics.
/// </summary>
public class Servers
{
    /// <summary>
    /// Gets the interval in seconds between observations of server instances for the live Workbench view.
    /// </summary>
    public int ObserveIntervalSeconds { get; init; } = 5;
}
