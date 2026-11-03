// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Defines the atomic reservation of the keys a new owner uses, shared by all silos.
/// </summary>
public interface IResourceNameReservations
{
    /// <summary>
    /// Reserve keys for an owner. Either all keys are held by the owner afterwards, or none are changed.
    /// </summary>
    /// <param name="owner">The owner, as the exact names it was created with.</param>
    /// <param name="keys">The keys to reserve.</param>
    /// <returns>True if all keys are held by the owner, false if any key belongs to a different owner.</returns>
    /// <remarks>Reserving a key the owner already holds is not a conflict.</remarks>
    Task<bool> Reserve(string owner, IReadOnlyCollection<string> keys);
}
