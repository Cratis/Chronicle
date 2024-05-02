// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Configuration;

/// <summary>
/// Represents the storage configuration.
/// </summary>
public class Storage
{
    /// <summary>
    /// The type of storage used.
    /// </summary>
    public string Type { get; init; } = "Not Configured";

    /// <summary>
    /// Gets the provider type specific connection details.
    /// </summary>
    public object ConnectionDetails { get; init; } = "mongodb://localhost:27017/cratis";
}
