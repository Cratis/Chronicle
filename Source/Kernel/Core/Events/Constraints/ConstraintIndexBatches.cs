// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Collects the unique constraint index changes of appended events, per constraint, so each constraint's index is
/// written once for the whole append rather than once per event.
/// </summary>
internal sealed class ConstraintIndexBatches
{
    readonly Dictionary<UniqueConstraintValidator, List<UniqueConstraintIndexChange>> _changes = [];

    /// <summary>
    /// Gets the collected changes per constraint, each in append order.
    /// </summary>
    public IEnumerable<(UniqueConstraintValidator Validator, IReadOnlyList<UniqueConstraintIndexChange> Changes)> Batches =>
        _changes.Where(_ => _.Value.Count > 0).Select(_ => (_.Key, (IReadOnlyList<UniqueConstraintIndexChange>)_.Value));

    /// <summary>
    /// Collects the change an update makes, when the constraint can apply changes together.
    /// </summary>
    /// <param name="validator">The <see cref="IConstraintValidator"/> owning the index.</param>
    /// <param name="updater">The <see cref="IUpdateConstraintIndex"/> for the event.</param>
    /// <param name="sequenceNumber">The <see cref="EventSequenceNumber"/> of the event.</param>
    /// <returns>True when the update is taken care of by the batch, false when it has to be made on its own.</returns>
    public bool TryAdd(IConstraintValidator validator, IUpdateConstraintIndex updater, EventSequenceNumber sequenceNumber)
    {
        if (validator is not UniqueConstraintValidator { CanApplyIndexChangesTogether: true } unique || updater is not UniqueConstraintIndexUpdater uniqueUpdater)
        {
            return false;
        }

        // An event that leaves the index as it is has nothing to write - the single update returns without writing too.
        if (uniqueUpdater.Describe(sequenceNumber) is { } change)
        {
            if (!_changes.TryGetValue(unique, out var changes))
            {
                _changes[unique] = changes = [];
            }

            changes.Add(change);
        }

        return true;
    }
}
