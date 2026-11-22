// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

/// <summary>
/// Holds one constraint's v2 claims; every access is inside the owning store's authority gate.
/// </summary>
internal sealed class UniqueConstraintV2Claims
{
    readonly Dictionary<(UniqueConstraintStorageScope Scope, UniqueConstraintValue Value), Claim> _values = [];

    /// <summary>
    /// Checks ownership without claiming a value.
    /// </summary>
    /// <param name="owner">The proposed owner.</param>
    /// <param name="value">The opaque value.</param>
    /// <param name="scope">The opaque scope.</param>
    /// <returns>Admission and the original sequence, or unavailable.</returns>
    internal (bool IsAllowed, EventSequenceNumber SequenceNumber) IsAllowed(EventSourceId owner, UniqueConstraintValue value, UniqueConstraintStorageScope scope) =>
        _values.TryGetValue((scope, value), out var claim)
            ? (claim.Owner == owner, claim.SequenceNumber)
            : (true, EventSequenceNumber.Unavailable);

    /// <summary>
    /// Snapshots the owner's values in one scope.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="scope">The opaque scope.</param>
    /// <returns>A detached snapshot.</returns>
    internal UniqueConstraintValue[] GetFor(EventSourceId owner, UniqueConstraintStorageScope scope) =>
        _values.Where(_ => _.Key.Scope == scope && _.Value.Owner == owner).Select(_ => _.Key.Value).ToArray();

    /// <summary>
    /// Claims a value before releasing a previous per-source value.
    /// </summary>
    /// <param name="mode">The retention mode.</param>
    /// <param name="value">The opaque value.</param>
    /// <param name="scope">The opaque scope.</param>
    /// <param name="claim">The owner, originating sequence and refreshed subject metadata.</param>
    /// <returns>A conflicting claim, or null on success.</returns>
    internal UniqueConstraintWriteFailure? Save(UniqueConstraintMode mode, UniqueConstraintValue value, UniqueConstraintStorageScope scope, Claim claim)
    {
        var key = (scope, value);
        if (_values.TryGetValue(key, out var existing))
        {
            if (existing.Owner != claim.Owner)
            {
                return UniqueConstraintWriteFailure.ConflictingClaim;
            }

            _values[key] = existing with { Subject = claim.Subject };
            return null;
        }

        if (mode == UniqueConstraintMode.PerEventSource)
        {
            Remove(claim.Owner, scope);
        }

        _values.Add(key, claim);
        return null;
    }

    /// <summary>
    /// Removes one owner's values in a scope.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="scope">The opaque scope.</param>
    /// <returns>Null on success, including an absent claim.</returns>
    internal UniqueConstraintWriteFailure? Remove(EventSourceId owner, UniqueConstraintStorageScope scope)
    {
        foreach (var key in _values.Where(_ => _.Key.Scope == scope && _.Value.Owner == owner).Select(_ => _.Key).ToArray())
        {
            _values.Remove(key);
        }

        return null;
    }

    /// <summary>
    /// Removes one owned per-value claim.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <param name="mode">The retention mode.</param>
    /// <param name="value">The opaque value.</param>
    /// <param name="scope">The opaque scope.</param>
    /// <returns>Null on success, including a no-op.</returns>
    internal UniqueConstraintWriteFailure? RemoveValue(EventSourceId owner, UniqueConstraintMode mode, UniqueConstraintValue value, UniqueConstraintStorageScope scope)
    {
        var key = (scope, value);
        if (mode != UniqueConstraintMode.PerValue)
        {
            return null;
        }

        if (_values.TryGetValue(key, out var claim) && claim.Owner == owner)
        {
            _values.Remove(key);
        }

        return null;
    }

    /// <summary>
    /// Clears one scope.
    /// </summary>
    /// <param name="scope">The opaque scope.</param>
    /// <returns>Null on success.</returns>
    internal UniqueConstraintWriteFailure? Clear(UniqueConstraintStorageScope scope)
    {
        foreach (var key in _values.Keys.Where(_ => _.Scope == scope).ToArray())
        {
            _values.Remove(key);
        }

        return null;
    }

    /// <summary>
    /// Clears all scopes in this constraint.
    /// </summary>
    /// <returns>Null on success.</returns>
    internal UniqueConstraintWriteFailure? Clear()
    {
        _values.Clear();
        return null;
    }

    /// <summary>
    /// Removes one subject across this constraint's scopes.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <returns>Null on success.</returns>
    internal UniqueConstraintWriteFailure? RemoveForSubject(Subject subject)
    {
        foreach (var key in _values.Where(_ => _.Value.Subject == subject).Select(_ => _.Key).ToArray())
        {
            _values.Remove(key);
        }

        return null;
    }

    /// <summary>
    /// Represents one owner's originating sequence and replaceable subject metadata.
    /// </summary>
    /// <param name="Owner">The owner.</param>
    /// <param name="SequenceNumber">The originating sequence.</param>
    /// <param name="Subject">The subject metadata, or null.</param>
    internal sealed record Claim(EventSourceId Owner, EventSequenceNumber SequenceNumber, Subject? Subject);
}
