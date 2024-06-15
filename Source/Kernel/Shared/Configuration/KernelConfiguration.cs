// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for the Kernel.
/// </summary>
public class KernelConfiguration
{
    /// <summary>
    /// Gets the <see cref="Storage"/> configuration.
    /// </summary>
    public Storage Storage { get; init; } = new();
}
