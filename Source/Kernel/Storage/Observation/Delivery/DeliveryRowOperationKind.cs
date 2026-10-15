// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Identifies the write semantics of a delivery row operation.
/// </summary>
public enum DeliveryRowOperationKind
{
    /// <summary>
    /// A strict insert; a collision rolls back the commit.
    /// </summary>
    Insert = 0,

    /// <summary>
    /// Creates or replaces an F row.
    /// </summary>
    Upsert = 1,

    /// <summary>
    /// An idempotent identity-only delete.
    /// </summary>
    Delete = 2
}
