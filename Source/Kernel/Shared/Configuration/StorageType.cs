// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the configuration for a specific shared storage type.
/// </summary>
public record StorageType
{
    /// <summary>
    /// The type of storage used.
    /// </summary>
    public string Type { get; init; } = "Not Configured";

    /// <summary>
    /// Gets the provider type specific connection details.
    /// </summary>
    public object ConnectionDetails { get; init; } = string.Empty;
}
