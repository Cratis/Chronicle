// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    /// <summary>
    /// The number of unique constraint changes written in one storage operation.
    /// </summary>
    const int ConstraintIndexBatchSize = 1000;

    static bool CanUpdateConstraintIndex(IConstraintValidator validator, ConstraintValidationContext context) => validator switch
    {
        // A no-op must not consume a write deadline or become pending when another constraint
        // exhausted the turn. That would make unrelated appends trigger extra repair writes.
        UniqueConstraintValidator => validator.CanValidate(context) || ((UniqueConstraintDefinition)validator.Definition).RemovedWith.Contains(context.EventTypeId),
        ClosesStreamConstraintValidator => validator.CanValidate(context),
        _ => true
    };

    async Task UpdateConstraintIndexes(IEnumerable<(ConstraintValidationContext Context, EventSequenceNumber SequenceNumber)> contexts)
    {
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(2), ConstraintIndexTimeProvider);
        var batches = new ConstraintIndexBatches();
        foreach (var (context, number) in contexts)
        {
            ExcludeLiveConstraintKeys(context, number);
            var updates = context.IndexUpdates.ToArray();
            if (number > _constraintIndexPosition) MarkConstraintIndexGap(_constraintIndexPosition);
            foreach (var (validator, updater) in updates.Where(update => CanUpdateConstraintIndex(update.Validator, context) && !_pendingConstraintIndexes.ContainsKey(update.Validator.Definition.Name)))
            {
                // Unique constraint changes are collected and written together after the loop, one write per
                // constraint rather than one per event. Each constraint has an index of its own, so writing it
                // after the others changes nothing they see.
                if (batches.TryAdd(validator, updater, number)) continue;

                var name = validator.Definition.Name;
                var outcome = await AttemptConstraintIndexUpdate(updater, number, name, budget.Token);
                if (outcome is ConstraintIndexUpdateOutcome.Failed or ConstraintIndexUpdateOutcome.Abandoned or ConstraintIndexUpdateOutcome.NotAttempted)
                {
                    MarkConstraintIndexPending(name, number);
                }
            }
            if (number >= _constraintIndexPosition) _constraintIndexPosition = number.Next();
        }
        await ApplyConstraintIndexBatches(batches, budget.Token);
        UpdateConstraintIndexWatermark();
    }

    /// <summary>
    /// Writes the collected unique constraint changes, one write per constraint and chunk.
    /// </summary>
    /// <param name="batches">The collected <see cref="ConstraintIndexBatches"/>.</param>
    /// <param name="cancellationToken">The deadline of the turn.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A chunk that cannot be written leaves the constraint pending from its first event, exactly as a failed single
    /// update does, and the chunks after it are left to the repair that follows. Every chunk before it is written.
    /// </remarks>
    async Task ApplyConstraintIndexBatches(ConstraintIndexBatches batches, CancellationToken cancellationToken)
    {
        foreach (var (validator, changes) in batches.Batches)
        {
            var name = validator.Definition.Name;
            foreach (var chunk in changes.Chunk(ConstraintIndexBatchSize))
            {
                IReadOnlyCollection<int> refused = [];
                var outcome = await AttemptConstraintIndexWrite(async token => refused = await validator.ApplyIndexChanges(chunk, token), chunk[0].SequenceNumber, name, cancellationToken);
                if (outcome is ConstraintIndexUpdateOutcome.Failed or ConstraintIndexUpdateOutcome.Abandoned or ConstraintIndexUpdateOutcome.NotAttempted)
                {
                    MarkConstraintIndexPending(name, chunk[0].SequenceNumber);
                    break;
                }

                foreach (var index in refused)
                {
                    ObserveConstraintIndexUpdate(ConstraintIndexUpdateOutcome.Settled, new DuplicateUniqueConstraintValue(name, chunk[index].EventSourceId), name, chunk[index].SequenceNumber);
                }
            }
        }
    }

    Task<ConstraintIndexUpdateOutcome> AttemptConstraintIndexUpdate(
        IUpdateConstraintIndex updater,
        EventSequenceNumber number,
        Concepts.Events.Constraints.ConstraintName name,
        CancellationToken cancellationToken) =>
        AttemptConstraintIndexWrite(token => updater.Update(number, token), number, name, cancellationToken);

    async Task<ConstraintIndexUpdateOutcome> AttemptConstraintIndexWrite(
        Func<CancellationToken, Task> write,
        EventSequenceNumber number,
        Concepts.Events.Constraints.ConstraintName name,
        CancellationToken cancellationToken)
    {
        var outcome = ConstraintIndexUpdateOutcome.NotAttempted;
        for (var attempt = 0; attempt < 3 && !cancellationToken.IsCancellationRequested; attempt++)
        {
            if (attempt > 0 && !await WaitBeforeConstraintIndexRetry(attempt, cancellationToken)) return ConstraintIndexUpdateOutcome.NotAttempted;
            (outcome, var error) = await ConstraintIndexUpdateAttempt.Run(write, cancellationToken, ConstraintIndexTimeProvider);
            ObserveConstraintIndexUpdate(outcome, error, name, number);
            if (outcome is not (ConstraintIndexUpdateOutcome.Failed or ConstraintIndexUpdateOutcome.Abandoned)) break;
        }

        // An unconfirmed attempt must be folded again in a later timer turn even if a retry succeeded.
        return _abandonedConstraintIndexes.Contains(name) ? ConstraintIndexUpdateOutcome.Abandoned : outcome;
    }

    async Task<bool> WaitBeforeConstraintIndexRetry(int attempt, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(attempt == 1 ? 100 : 400), ConstraintIndexTimeProvider, cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    void ObserveConstraintIndexUpdate(ConstraintIndexUpdateOutcome outcome, Exception? error, Concepts.Events.Constraints.ConstraintName name, EventSequenceNumber number)
    {
        if (error is not null)
        {
            logger.ConstraintIndexUpdateOutcome(_eventSequenceKey, name, number, State.ConstraintIndexWatermark!, State.SequenceNumber, outcome, error);
            if (outcome is ConstraintIndexUpdateOutcome.Failed or ConstraintIndexUpdateOutcome.Abandoned or ConstraintIndexUpdateOutcome.NotAttempted) _metrics?.ConstraintIndexUpdateFailed();
        }
        if (outcome == ConstraintIndexUpdateOutcome.SettledWithError) _metrics?.ConstraintIndexSettledWithError();
        if (outcome == ConstraintIndexUpdateOutcome.Abandoned) _abandonedConstraintIndexes.Add(name);
    }
}
