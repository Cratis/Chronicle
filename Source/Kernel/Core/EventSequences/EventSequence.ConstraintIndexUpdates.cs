// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    async Task UpdateConstraintIndexes(IEnumerable<(ConstraintValidationContext Context, EventSequenceNumber SequenceNumber)> contexts)
    {
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(2), ConstraintIndexTimeProvider);
        foreach (var (context, number) in contexts)
        {
            ExcludeLiveConstraintKeys(context, number);
            var updates = context.IndexUpdates.ToArray();
            if (number > _constraintIndexPosition) MarkConstraintIndexGap(_constraintIndexPosition);
            foreach (var (validator, updater) in updates.Where(update => !_pendingConstraintIndexes.ContainsKey(update.Validator.Definition.Name)))
            {
                var name = validator.Definition.Name;
                var outcome = await AttemptConstraintIndexUpdate(updater, number, name, budget.Token);
                if (outcome is ConstraintIndexUpdateOutcome.Failed or ConstraintIndexUpdateOutcome.Abandoned or ConstraintIndexUpdateOutcome.NotAttempted)
                {
                    MarkConstraintIndexPending(name, number);
                }
            }
            if (number >= _constraintIndexPosition) _constraintIndexPosition = number.Next();
        }
        UpdateConstraintIndexWatermark();
    }

    async Task<ConstraintIndexUpdateOutcome> AttemptConstraintIndexUpdate(
        IUpdateConstraintIndex updater,
        EventSequenceNumber number,
        Concepts.Events.Constraints.ConstraintName name,
        CancellationToken cancellationToken)
    {
        var outcome = ConstraintIndexUpdateOutcome.NotAttempted;
        for (var attempt = 0; attempt < 3 && !cancellationToken.IsCancellationRequested; attempt++)
        {
            if (attempt > 0)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(attempt == 1 ? 100 : 400), ConstraintIndexTimeProvider, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return ConstraintIndexUpdateOutcome.NotAttempted;
                }
            }
            (outcome, var error) = await ConstraintIndexUpdateAttempt.Run(token => updater.Update(number, token), cancellationToken, ConstraintIndexTimeProvider);
            if (error is not null)
            {
                logger.ConstraintIndexUpdateOutcome(_eventSequenceKey, name, number, State.ConstraintIndexWatermark!, State.SequenceNumber, outcome, error);
                if (outcome is ConstraintIndexUpdateOutcome.Failed or ConstraintIndexUpdateOutcome.Abandoned or ConstraintIndexUpdateOutcome.NotAttempted) _metrics?.ConstraintIndexUpdateFailed();
            }
            if (outcome == ConstraintIndexUpdateOutcome.SettledWithError) _metrics?.ConstraintIndexSettledWithError();
            if (outcome == ConstraintIndexUpdateOutcome.Abandoned) _abandonedConstraintIndexes.Add(name);
            if (outcome is not (ConstraintIndexUpdateOutcome.Failed or ConstraintIndexUpdateOutcome.Abandoned)) break;
        }

        // An unconfirmed attempt must be folded again in a later timer turn even if a retry succeeded.
        return _abandonedConstraintIndexes.Contains(name) ? ConstraintIndexUpdateOutcome.Abandoned : outcome;
    }
}
