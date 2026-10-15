// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;
using Cratis.Monads;

using EffectReceipt = Cratis.Chronicle.Occurrences.EffectReceipt;
using StoredEffectReceipt = Cratis.Chronicle.Storage.Occurrences.EffectReceipt;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    IEffectReceiptStorage EffectReceiptStorage => EventSequenceStorage as IEffectReceiptStorage ?? throw new OccurrencesStorageNotSupported();

    /// <inheritdoc/>
    public Task<AppendManyResult> AppendManyWithEffectReceipt(
        IEnumerable<EventToAppend> events,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        ConcurrencyScopes concurrencyScopes,
        EffectReceipt effectReceipt) =>
        AppendManyValidated(events, correlationId, causation, causedBy, concurrencyScopes, effectReceipt);

    /// <inheritdoc/>
    public async Task<RecordEffectReceiptResult> RecordEffectReceipt(EffectReceipt effectReceipt, EffectOutcome outcome, IReadOnlyList<string> reasons)
    {
        var id = ReceiptId(effectReceipt);
        var existing = await EffectReceiptStorage.GetEffectReceipt(id);
        if (existing is not null) return new(EffectReceiptStatus.AlreadyRecorded, EffectReceiptConverters.ToDetails(existing));
        ValidateEffectReceipt(effectReceipt);
        if (outcome is not (EffectOutcome.NoEvents or EffectOutcome.Refused))
        {
            throw new InvalidEffectReceipt("Receipt-only writes must be NoEvents or Refused.");
        }

        var receipt = new StoredEffectReceipt(id, new(new(effectReceipt.OccurrenceId), effectReceipt.Epoch), outcome, null, null, null, reasons, effectReceipt.Response, DateTimeOffset.UtcNow);
        var result = await WriteEffectReceiptForSettlement([], receipt);
        if (result == EffectReceiptWriteResult.Written) return new(EffectReceiptStatus.Recorded, EffectReceiptConverters.ToDetails(receipt));
        existing = await EffectReceiptStorage.GetEffectReceipt(id);
        if (existing is not null) return new(EffectReceiptStatus.AlreadyRecorded, EffectReceiptConverters.ToDetails(existing));

        // The command executor carries exceptions as a failed CommandResult on the wire, not a success status.
        throw new EffectReceiptUnsettled();
    }

    async Task<AppendManyResult?> FindEffectReceipt(EffectReceipt effectReceipt, CorrelationId correlationId)
    {
        var receipt = await EffectReceiptStorage.GetEffectReceipt(ReceiptId(effectReceipt));
        if (receipt is null) return null;
        await ReconcileEffectReceipt(receipt);

        return AlreadyRecorded(receipt, correlationId);
    }

    AppendManyResult AlreadyRecorded(StoredEffectReceipt receipt, CorrelationId correlationId) => new()
    {
        CorrelationId = correlationId,
        EffectAlreadyRecorded = true,
        EffectReceipt = EffectReceiptConverters.ToDetails(receipt)
    };

    async Task ReconcileEffectReceipt(StoredEffectReceipt receipt)
    {
        if (receipt.Outcome != EffectOutcome.Appended || receipt.EventSequenceId != _eventSequenceId || HasAccountedForEffectReceipt(receipt)) return;
        var first = receipt.FirstSequenceNumber!;
        var last = receipt.LastSequenceNumber!;
        await RefreshConstraintsIfChanged();
        var appendedEvents = new List<AppendedEvent>();
        var updates = new List<(ConstraintValidationContext Context, EventSequenceNumber SequenceNumber)>();
        for (var number = first; number <= last; number = number.Next())
        {
            var @event = await EventSequenceStorage.GetEventAt(number);
            appendedEvents.Add(@event);
            var context = @event.Context;
            if (!State.TailSequenceNumberPerEventType.TryGetValue(context.EventType.Id, out var tail) || tail < number)
            {
                State.TailSequenceNumberPerEventType[context.EventType.Id] = number;
            }

            // Establish index updates from the durable winner, never from the retry's possibly different payload.
            // Like reindexing, release protected content before hashing constraint values.
            var schema = await EventTypesStorage.GetFor(context.EventType.Id, context.EventType.Generation);
            var content = @event.Content;
            if (schema.Schema.HasSchemaMetadata())
            {
                var subject = context.Subject.IsSet ? context.Subject.Value : context.EventSourceId.Value;
                var json = expandoObjectConverter.ToJsonObject(content, schema.Schema);
                var released = await jsonComplianceManagerProvider.Release(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, schema.Schema, subject, json);
                content = expandoObjectConverter.ToExpandoObject(released, schema.Schema);
            }

            updates.Add((_constraints!.Establish(context.EventSourceId, context.EventType.Id, content, context.EventSourceType, context.EventStreamType, context.EventStreamId), number));
        }

        State.SequenceNumber = last.Next();
        await CompleteDurableAppend(appendedEvents, updates);
    }

    bool HasAccountedForEffectReceipt(StoredEffectReceipt receipt)
    {
        // Only the commit-before-ack window needs repair. Replaying an accounted-for range
        // would apply historical constraint claims over newer releases and dispatch events twice.
        return receipt.Outcome == EffectOutcome.Appended && State.SequenceNumber > receipt.LastSequenceNumber!;
    }

    async Task<Result<IEnumerable<AppendedEvent>, AppendManyResult>> AppendEffectToStorage(
        List<EventToAppendToStorage> events,
        EffectReceipt effectReceipt,
        CorrelationId correlationId)
    {
        while (true)
        {
            var result = await WriteEffectReceiptForSettlement(events, PrepareEffectReceipt(effectReceipt, events));
            if (result == EffectReceiptWriteResult.Written)
            {
                var receipt = await EffectReceiptStorage.GetEffectReceipt(ReceiptId(effectReceipt));
                if (receipt is null)
                {
                    return new AppendManyResult { CorrelationId = correlationId, Errors = [AppendError.EffectReceiptUnsettled] };
                }

                // Apply the same range guard after Written as after an ambiguous commit: an
                // acknowledged historical winner must not roll the grain or constraint index back.
                if (HasAccountedForEffectReceipt(receipt)) return AlreadyRecorded(receipt, correlationId);
                break;
            }
            var settled = await FindEffectReceipt(effectReceipt, correlationId);
            if (settled is not null) return settled;
            if (result != EffectReceiptWriteResult.DuplicateEventSequenceNumber)
            {
                return new AppendManyResult { CorrelationId = correlationId, Errors = [AppendError.EffectReceiptUnsettled] };
            }

            var next = (await EventSequenceStorage.GetTailSequenceNumber()).Next();
            await RenumberAppendMany(events, new(next));
            if (await ValidateAppendManyRetry(correlationId) is { } failure) return failure;
        }

        var appendedEvents = new List<AppendedEvent>();
        foreach (var @event in events)
        {
            appendedEvents.Add(await EventSequenceStorage.GetEventAt(@event.SequenceNumber));
        }

        return Result<IEnumerable<AppendedEvent>, AppendManyResult>.Success(appendedEvents);
    }

    async Task<EffectReceiptWriteResult> WriteEffectReceiptForSettlement(IEnumerable<EventToAppendToStorage> events, StoredEffectReceipt receipt)
    {
        try
        {
            return await EffectReceiptStorage.AppendManyWithEffectReceipt(events, receipt);
        }
        catch (Exception exception)
        {
            // A storage exception cannot establish whether the commit happened. Both callers settle by
            // receipt lookup, reconciling the durable winner or reporting EffectReceiptUnsettled on a miss.
            logger.ErrorAppendingMany(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, exception);
            return EffectReceiptWriteResult.TransactionConflicted;
        }
    }

    StoredEffectReceipt PrepareEffectReceipt(EffectReceipt effectReceipt, List<EventToAppendToStorage> events) => new(
        ReceiptId(effectReceipt),
        new(new(effectReceipt.OccurrenceId), effectReceipt.Epoch),
        events.Count > 0 ? EffectOutcome.Appended : EffectOutcome.NoEvents,
        events.Count > 0 ? _eventSequenceId : null,
        events.Count > 0 ? events[0].SequenceNumber : null,
        events.Count > 0 ? events[^1].SequenceNumber : null,
        [],
        effectReceipt.Response,
        DateTimeOffset.UtcNow);
}
