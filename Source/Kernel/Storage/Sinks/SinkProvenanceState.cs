// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents committed, monotonic database-local inventory facts.
/// </summary>
/// <param name="DatabaseId">The physical database identity.</param>
/// <param name="InventoryVersion">The completed inventory version, or zero before completion.</param>
/// <param name="Restricted">Whether restricted evidence has been preserved.</param>
public sealed record SinkProvenanceState(SinkProvenanceDatabaseId DatabaseId, int InventoryVersion, bool Restricted)
{
    /// <summary>
    /// Gets the supported inventory version.
    /// </summary>
    public const int CurrentInventoryVersion = 1;

    /// <summary>
    /// Gets whether the supported inventory has completed.
    /// </summary>
    public bool IsComplete => InventoryVersion == CurrentInventoryVersion;

    /// <summary>
    /// Validates the supported state representation.
    /// </summary>
    /// <returns>The validated state.</returns>
    /// <exception cref="InvalidOperationException">The state is unsupported or malformed.</exception>
    public SinkProvenanceState Validate() => DatabaseId.Value != Guid.Empty && InventoryVersion is 0 or CurrentInventoryVersion
        ? this
        : throw new InvalidOperationException("Read-model storage is not available.");
}
