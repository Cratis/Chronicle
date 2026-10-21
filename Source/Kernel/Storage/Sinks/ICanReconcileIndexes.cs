// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Optional sink capability for reconciling indexes after a read-model definition changes.
/// Routine activation must only ensure indexes, never remove undeclared indexes.
/// </summary>
public interface ICanReconcileIndexes
{
    /// <summary>
    /// Applies the index declarations of the changed read-model definition.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task ReconcileIndexes();
}
