// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the client configuration.
/// </summary>
[Configuration("cratis")]
public class ClientConfiguration
{
    /// <summary>
    /// Gets the Kernel connectivity configuration.
    /// </summary>
    public KernelConnectivity Kernel { get; init; } = new();
}
