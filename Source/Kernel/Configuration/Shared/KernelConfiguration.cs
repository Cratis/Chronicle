// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Extensions.Orleans.Configuration;

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the configuration for the Kernel.
/// </summary>
[Configuration("cratis")]
public class KernelConfiguration
{
    /// <summary>
    /// Gets the <see cref="Tenants"/> configuration.
    /// </summary>
    public Tenants Tenants { get; init; } = new();

    /// <summary>
    /// Gets the <see cref="Cluster"/> configuration.
    /// </summary>
    public Cluster Cluster { get; init; } = new();

    /// <summary>
    /// Gets the <see cref="Storage"/> configuration.
    /// </summary>
    public Storage Storage { get; init; } = new();
}
