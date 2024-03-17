// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Configuration;
using Cratis.Kernel.Orleans.Configuration;

namespace Cratis.Kernel.Configuration;

/// <summary>
/// Represents the configuration for the Kernel.
/// </summary>
[Configuration("cratis")]
public class KernelConfiguration
{
    /// <summary>
    /// Gets the <see cref="Telemetry"/> configuration.
    /// </summary>
    public Telemetry Telemetry { get; init; } = new();

    /// <summary>
    /// Gets the <see cref="Storage"/> configuration.
    /// </summary>
    public Storage Storage { get; init; } = new();
}
