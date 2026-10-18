// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences.Idempotency;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventSequences;

public partial class EventSequence
{
    IEventPublicationStorage PublicationStorage => EventSequenceStorage as IEventPublicationStorage ?? throw new EventPublicationStorageNotSupported();

    /// <inheritdoc/>
    public async Task<IdempotentAppendResult> AppendWithIdempotencyKey(EventToAppend @event, IdempotencyKey idempotencyKey, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScope concurrencyScope)
    {
        var requests = new List<KeyedAppend> { new(@event, idempotencyKey) };
        var scopes = new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope> { [@event.EventSourceId] = concurrencyScope });
        await AppendKeyedBatch(requests, correlationId, causation, causedBy, scopes);
        return requests[0].Result!;
    }

    static IdempotentAppendResult ComparedKey(KeyedAppend request, EventSequenceNumber number, FingerprintComparisonResult comparison, CorrelationId correlationId)
    {
        if (comparison.Status is FingerprintComparisonStatus.Verified or FingerprintComparisonStatus.VerifiedAfterErasure)
        {
            return new(
                AppendResult.Success(correlationId, number),
                comparison.Status == FingerprintComparisonStatus.Verified ? AppendOutcome.AlreadyAppended : AppendOutcome.AlreadyAppendedAfterErasure);
        }
        var reason = comparison.Status switch
        {
            FingerprintComparisonStatus.Unverifiable => IdempotencyConflictReason.Unverifiable,
            _ => comparison.Reason == FingerprintConflictReason.Generation ? IdempotencyConflictReason.Generation : IdempotencyConflictReason.Content
        };
        var conflict = new IdempotencyConflict(request.Key!, number, comparison.StoredEventType, request.Event.EventType.Generation, reason);
        return new(AppendResult.Failed(correlationId, [reason == IdempotencyConflictReason.Unverifiable
            ? IdempotencyRefusals.Unverifiable(request.Key!, comparison.StoredEventType, number, comparison.UnverifiableKind)
            : IdempotencyRefusals.Conflict(conflict)])) { Conflicts = [conflict] };
    }

    static void FinishAbsentKeys(IEnumerable<KeyedAppend> requests, AppendResult failure)
    {
        foreach (var request in requests.Where(request => request.Result is null)) request.Result = new(failure);
    }

    async Task AppendKeyedBatch(List<KeyedAppend> requests, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopes scopes)
    {
        try
        {
            await SettleKeys(requests, correlationId);
            if (requests.TrueForAll(request => request.Result is not null)) return;
            if (requests.Exists(request => request.Result?.IsSuccess == false))
            {
                FinishAbsentKeys(requests, requests.First(request => request.Result?.IsSuccess == false).Result!.Result);
                return;
            }

            var absent = requests.Where(request => request.Result is null).ToList();
            await RefreshConstraintsIfChanged();
            if (await RefusedByPendingConstraintIndex(absent.Select(request => request.Event.EventType.Id).ToArray()))
            {
                await SettleFailedKeys(requests, correlationId, AppendResult.Failed(correlationId, [AppendError.ConstraintIndexUnavailable]));
                return;
            }

            var uses = absent.Select(request => request.Key is null ? null : new List<ProtectedValueUse>()).ToArray();
            var candidates = await ValidateBatchEvents(absent.Select(request => request.Event), correlationId, new ConstraintBatchClaims(), uses);
            var failures = candidates.Where(candidate => !candidate.Result.IsSuccess).Select(candidate => candidate.Result.AsT1).ToArray();
            if (failures.Length > 0)
            {
                await SettleFailedKeys(requests, correlationId, new AppendResult
                {
                    CorrelationId = correlationId,
                    Errors = failures.SelectMany(failure => failure.Errors).ToArray(),
                    ConstraintViolations = failures.SelectMany(failure => failure.ConstraintViolations).ToArray()
                });
                return;
            }
            var validated = new List<(EventToAppend Event, ExpandoObject CompliantEvent, JsonObject CompliantContent, ConstraintValidationContext ConstraintContext)>();
            for (var index = 0; index < absent.Count; index++)
            {
                var request = absent[index];
                if (request.Key is not null)
                {
                    var fingerprint = AppendFingerprint.Compute(request.Input!, uses[index]!.ToArray());
                    if (fingerprint.TryGetError(out var failure))
                    {
                        var error = failure == FingerprintFailure.FingerprintKeysDiverged ? IdempotencyRefusals.Unsettled(request.Key) : IdempotencyRefusals.Unsupported(request.Key);
                        await SettleFailedKeys(requests, correlationId, AppendResult.Failed(correlationId, [error]));
                        return;
                    }
                    request.Fingerprint = fingerprint.AsT0;
                }
                var (value, content, context) = candidates[index].Result.AsT0;
                validated.Add((candidates[index].Event, value, content, context));
            }

            scopes = RemainingScopes(absent, scopes);
            using var concurrencyCheck = BeginConcurrencyValidation(scopes);
            var violations = (await ConcurrencyValidator.Validate(scopes)).ToArray();
            if (violations.Length > 0)
            {
                await SettleConcurrencyFailure(requests, correlationId, violations, scopes.ShouldAllBeValidated);
                return;
            }
            var identity = await IdentityStorage.GetFor(causedBy.WithoutDuplicates());
            var (batch, contexts) = await BuildStorageBatch(validated, correlationId, causation, identity);
            for (var index = 0; index < batch.Count; index++)
            {
                if (absent[index].Key is { } key)
                    batch[index] = batch[index] with { Identity = new EventPublication(key.Identity, absent[index].Fingerprint!.Format()) };
            }
            await StoreKeyedBatch(absent, batch, contexts, correlationId, scopes);
        }
        catch (Exception error)
        {
            var @event = requests[0].Event;
            FinishAbsentKeys(requests, HandleAppendEventException(error, @event.EventSourceType, @event.EventSourceId, @event.EventType, @event.eventStreamId, correlationId));
        }
    }

    async Task SettleKeys(List<KeyedAppend> requests, CorrelationId correlationId)
    {
        var pending = requests.Where(request => request.Result is null && request.Key is not null).ToArray();
        if (pending.Length == 0) return;
        var receipts = await PublicationStorage.TryGetPublications(pending.Select(request => request.Key!.Identity));
        foreach (var request in pending)
        {
            var key = request.Key!;
            receipts.TryGetValue(key.Identity, out var receipt);
            StoredAppendFingerprint? stored = null;
            if (receipt is not null)
            {
                var parsed = StoredAppendFingerprint.Parse(receipt.Fingerprint);
                if (parsed.IsSuccess) stored = parsed.AsT0;
                await AccountForDurableKey(receipt, stored);
                if (stored is null)
                {
                    request.Result = new(AppendResult.Failed(correlationId, [IdempotencyRefusals.Malformed(key)]));
                    logger.KeyedAppendRefused(key, receipt.SequenceNumber);
                    continue;
                }
            }
            if (request.Input is null)
            {
                var @event = request.Event;
                var schema = await EventTypesStorage.GetFor(@event.EventType.Id, @event.EventType.Generation);
                var subject = @event.Subject?.IsSet == true ? @event.Subject.Value : @event.EventSourceId.Value;
                var locations = await AppendFingerprint.LocateProtectedValues(jsonComplianceManagerProvider, schema.Schema, subject, @event.Content);
                if (!locations.IsSuccess)
                {
                    request.Result = new(AppendResult.Failed(correlationId, [IdempotencyRefusals.Unsupported(key)]));
                    continue;
                }
                request.Input = new(@event.EventType, @event.Content, @event.EventSourceId, @event.EventSourceType, @event.eventStreamType, @event.eventStreamId, @event.EventSource, subject, key.Identity, locations.AsT0);
            }
            if (receipt is null) continue;
            var keys = encryptionKeyStorage ?? ServiceProvider.GetRequiredService<IEncryptionKeyStorage>();
            var comparison = await AppendFingerprintComparison.Compare(stored!, request.Input, keys, keys as IEvictEncryptionKeyCache ?? new NoKeyCache(), _eventSequenceKey.EventStore, _eventSequenceKey.Namespace);
            request.Result = comparison.IsSuccess
                ? ComparedKey(request, receipt.SequenceNumber, comparison.AsT0, correlationId)
                : new(AppendResult.Failed(correlationId, [IdempotencyRefusals.Unsupported(key)]));
            if (request.Result.IsSuccess) logger.KeyedAppendSettled(key, receipt.SequenceNumber);
            else logger.KeyedAppendRefused(key, receipt.SequenceNumber);
        }
    }

    async Task AccountForDurableKey(StoredEventPublication receipt, StoredAppendFingerprint? fingerprint)
    {
        if (receipt.SequenceNumber >= State.SequenceNumber)
        {
            _appendTailUncertain = true;
            await RefreshUncertainAppendTail();
            if (State.SequenceNumber <= receipt.SequenceNumber) State.SequenceNumber = receipt.SequenceNumber.Next();
        }
        if (fingerprint is not null && (!State.TailSequenceNumberPerEventType.TryGetValue(fingerprint.EventType.Id, out var tail) || tail < receipt.SequenceNumber))
            State.TailSequenceNumberPerEventType[fingerprint.EventType.Id] = receipt.SequenceNumber;
    }

    async Task SettleFailedKeys(List<KeyedAppend> requests, CorrelationId correlationId, AppendResult failure)
    {
        foreach (var request in requests.Where(request => request.Result is null && request.Key is not null)) logger.KeyedAppendSettlingAgain(request.Key!, State.SequenceNumber);
        await SettleKeys(requests, correlationId);
        FinishAbsentKeys(requests, failure);
    }

    async Task SettleConcurrencyFailure(List<KeyedAppend> requests, CorrelationId correlationId, IEnumerable<ConcurrencyViolation> violations, bool checkedScopes)
    {
        var all = violations.ToArray();
        await SettleKeys(requests, correlationId);
        foreach (var request in requests.Where(request => request.Result is null))
        {
            request.ConcurrencyViolations = all;
            request.Result = new(AppendResult.Failed(correlationId, all[0]).ReportingConcurrencyCheck(checkedScopes));
        }
    }

    sealed class KeyedAppend(EventToAppend @event, IdempotencyKey? key)
    {
        public EventToAppend Event { get; } = @event;
        public IdempotencyKey? Key { get; } = key;
        public IEnumerable<ConcurrencyViolation> ConcurrencyViolations { get; set; } = [];
        public AppendFingerprintInput? Input { get; set; }
        public StoredAppendFingerprint? Fingerprint { get; set; }
        public IdempotentAppendResult? Result { get; set; }
    }

    sealed class NoKeyCache : IEvictEncryptionKeyCache
    {
        public void EvictFromCache(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
        {
        }
    }
}
