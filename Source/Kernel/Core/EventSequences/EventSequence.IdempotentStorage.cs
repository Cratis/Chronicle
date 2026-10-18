// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    async Task StoreKeyedBatch(List<KeyedAppend> requests, List<EventToAppendToStorage> batch, List<ConstraintValidationContext> contexts, CorrelationId correlationId, bool concurrencyChecked)
    {
        var retriedNonCommit = false;
        while (batch.Count > 0)
        {
            Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure> result;
            try
            {
                result = await PublicationStorage.AppendManyWithIdentities(batch);
            }
            catch (EventsNotCommitted)
            {
                result = (AppendManyStorageFailure)new IdentitiesAlreadyDurable(new Dictionary<string, StoredEventPublication>());
            }
            catch (Exception)
            {
                await SettleUnknownKeyedAppend(requests, batch, contexts, correlationId, concurrencyChecked);
                return;
            }

            if (result.IsSuccess)
            {
                var settled = await SettleAppendedBatch(result.AsT0.ToList(), batch, correlationId, null);
                if (settled.Failure is not null)
                {
                    await SettleUnknownKeys(requests, correlationId);
                    return;
                }
                await CompleteKeyedAppend(requests, batch, contexts, settled.Events, correlationId, concurrencyChecked);
                return;
            }

            var failure = result.AsT1;
            if (failure.IsT0)
            {
                HandleAppendedDuplicateEventForMany(batch, failure.AsT0.NextAvailableSequenceNumber);
                if (await ValidateAppendManyRetry(correlationId) is { } violation)
                {
                    await SettleFailedKeys(requests, correlationId, new AppendResult
                    {
                        CorrelationId = correlationId,
                        ConstraintViolations = violation.ConstraintViolations,
                        ConcurrencyViolation = violation.ConcurrencyViolations.FirstOrDefault(),
                        Errors = violation.Errors,
                        ConcurrencyCheckPerformed = concurrencyChecked
                    });
                    return;
                }
                continue;
            }

            // A reported non-commit permits exactly one write of the still-absent remainder.
            await SettleKeys(requests, correlationId);
            if (requests.Exists(request => request.Result?.IsSuccess == false))
            {
                FinishAbsentKeys(requests, requests.First(request => request.Result?.IsSuccess == false).Result!.Result);
                return;
            }
            if (requests.TrueForAll(request => request.Result is not null)) return;
            if (retriedNonCommit)
            {
                foreach (var request in requests.Where(request => request.Result is null))
                    request.Result = new(AppendResult.Failed(correlationId, [IdempotencyRefusals.Unsettled(request.Key)]));
                return;
            }
            retriedNonCommit = true;
            for (var index = requests.Count - 1; index >= 0; index--)
            {
                if (requests[index].Result is null) continue;
                requests.RemoveAt(index);
                batch.RemoveAt(index);
                contexts.RemoveAt(index);
            }
            _appendTailUncertain = true;
            await RefreshUncertainAppendTail();
            var number = State.SequenceNumber;
            for (var index = 0; index < batch.Count; index++)
            {
                batch[index] = batch[index] with { SequenceNumber = number };
                number = number.Next();
            }
            if (await ValidateAppendManyRetry(correlationId) is { } retryViolation)
            {
                await SettleFailedKeys(requests, correlationId, new AppendResult
                {
                    CorrelationId = correlationId,
                    ConcurrencyViolation = retryViolation.ConcurrencyViolations.FirstOrDefault(),
                    ConcurrencyCheckPerformed = concurrencyChecked
                });
                return;
            }
        }
    }

    async Task SettleUnknownKeyedAppend(List<KeyedAppend> requests, List<EventToAppendToStorage> batch, List<ConstraintValidationContext> contexts, CorrelationId correlationId, bool concurrencyChecked)
    {
        await MarkAppendOutcomeUnknown(batch[0].SequenceNumber);
        var readBack = await ReadBackAppend(batch);
        if (readBack is not null)
        {
            await CompleteKeyedAppend(requests, batch, contexts, readBack, correlationId, concurrencyChecked);
            return;
        }
        await SettleUnknownKeys(requests, correlationId);
    }

    async Task SettleUnknownKeys(List<KeyedAppend> requests, CorrelationId correlationId)
    {
        try
        {
            await SettleKeys(requests, correlationId);
        }
        catch (Exception)
        {
            // A failed probe establishes neither a commit nor absence. Never report a non-commit here.
            FinishAbsentKeys(requests, AppendResult.Failed(correlationId, [AppendError.AppendOutcomeUnknown]));
            return;
        }
        FinishAbsentKeys(requests, AppendResult.Failed(correlationId, [AppendError.AppendOutcomeUnknown]));
    }

    async Task CompleteKeyedAppend(List<KeyedAppend> requests, List<EventToAppendToStorage> batch, List<ConstraintValidationContext> contexts, List<AppendedEvent> appended, CorrelationId correlationId, bool concurrencyChecked)
    {
        var next = batch[^1].SequenceNumber.Next();
        if (State.SequenceNumber < next) State.SequenceNumber = next;
        await CompleteConfirmedAppend(appended, contexts.Zip(batch, (context, @event) => (context, @event.SequenceNumber)));
        for (var index = 0; index < requests.Count; index++)
        {
            requests[index].Result = new(AppendResult.Success(correlationId, batch[index].SequenceNumber).ReportingConcurrencyCheck(concurrencyChecked));
            logger.KeyedAppendSettled(requests[index].Key, batch[index].SequenceNumber);
        }
    }
}
