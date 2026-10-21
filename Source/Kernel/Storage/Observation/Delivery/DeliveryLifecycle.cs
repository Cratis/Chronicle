// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Identifies delivery ownership. Unknown numeric values must be retained, not coerced.
/// </summary>
public enum DeliveryLifecycle
{
    /// <summary>
    /// Ownership is not known.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The root is locked for migration.
    /// </summary>
    MigrationLocked = 1,

    /// <summary>
    /// The ordered delivery engine owns the root.
    /// </summary>
    Engine = 2
}
