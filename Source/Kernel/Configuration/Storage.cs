// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the storage configuration.
/// </summary>
public class Storage
{
    /// <summary>
    /// Gets the type of storage used.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Gets the connection details for the storage.
    /// </summary>
    public string ConnectionDetails { get; init; } = string.Empty;
}
