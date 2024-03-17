// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Configuration;

namespace Cratis.Kernel.Configuration;

/// <summary>
/// Represents the storage configuration for all microservices.
/// </summary>
[Configuration]
public class Storage
{
    /// <summary>
    /// The type of storage used.
    /// </summary>
    public string Type { get; init; } = "Not Configured";

    /// <summary>
    /// Gets the provider type specific connection details.
    /// </summary>
    public object ConnectionDetails { get; init; } = string.Empty;
}
