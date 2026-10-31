// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Shares catch-up admission across all sequences using a silo's singleton storage service.
/// </summary>
internal sealed class ConstraintIndexCatchUpSlots
{
    static readonly ConditionalWeakTable<IStorage, ConstraintIndexCatchUpSlots> _silos = new();
    readonly object _gate = new();
    readonly HashSet<object> _memoryOwners = [];
    readonly HashSet<object> _turnOwners = [];
    readonly int _limit;

    ConstraintIndexCatchUpSlots(int limit) => _limit = limit;

    /// <summary>
    /// Gets the limit shared by a silo's sequences.
    /// </summary>
    /// <param name="storage">The silo's singleton storage service.</param>
    /// <param name="limit">The number of concurrent catch-ups; only the first call per storage service creates the instance, because one silo has one configuration.</param>
    /// <returns>The shared limit.</returns>
    internal static ConstraintIndexCatchUpSlots For(IStorage storage, int limit) =>
        _silos.TryGetValue(storage, out var slots) ? slots : Create(storage, limit);

    /// <summary>
    /// Tries to admit one bounded turn without overcommitting retained memory.
    /// </summary>
    /// <param name="owner">The sequence activation.</param>
    /// <returns>Whether the turn can run.</returns>
    internal bool TryEnter(object owner)
    {
        lock (_gate)
        {
            // A yielded fold retains its map. A separate memory lease prevents an arbitrary
            // number of queued activations retaining 30 MB each between folding turns.
            if (!_memoryOwners.Contains(owner) && _memoryOwners.Count >= _limit) return false;
            if (!_turnOwners.Contains(owner) && _turnOwners.Count >= _limit) return false;
            _memoryOwners.Add(owner);
            _turnOwners.Add(owner);
            return true;
        }
    }

    /// <summary>
    /// Releases the turn's slot unless writes are still under refusal.
    /// </summary>
    /// <param name="owner">The sequence activation.</param>
    /// <param name="refusing">Whether the durable refusal marker is set.</param>
    internal void LeaveTurn(object owner, bool refusing)
    {
        lock (_gate)
        {
            if (!refusing) _turnOwners.Remove(owner);
        }
    }

    /// <summary>
    /// Releases both retained memory and execution admission.
    /// </summary>
    /// <param name="owner">The completed or deactivating sequence.</param>
    internal void Complete(object owner)
    {
        lock (_gate)
        {
            _turnOwners.Remove(owner);
            _memoryOwners.Remove(owner);
        }
    }

    static ConstraintIndexCatchUpSlots Create(IStorage storage, int limit) => _silos.GetValue(storage, _ => new(limit));
}
