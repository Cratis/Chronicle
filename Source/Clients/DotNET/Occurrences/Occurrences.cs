// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Provides durable receipts in the event store's namespace.
/// </summary>
/// <param name="eventStore">The namespace-bound event store.</param>
public partial class Occurrences(IEventStore eventStore) : IOccurrences, IOccurrencesForUnitOfWork
{
    TriggerRegistry? _triggers;
    Func<Task>? _registerForTesting;
    JsonSerializerOptions _jsonOptions = JsonSerializerOptions.Default;

    /// <inheritdoc/>
    public async Task<OccurrenceRecordResult> Record(object trigger, string idempotencyId)
    {
        OccurrenceSupport.Require(eventStore.Connection);
        if (_registerForTesting is not null) await _registerForTesting();
        if (string.IsNullOrWhiteSpace(idempotencyId)) throw new TriggerNotValid("An occurrence idempotency identity must not be empty.");
        if (TriggerRegistry.IsLifecycle(trigger.GetType())) throw new TriggerNotValid("Lifecycle triggers are recorded by host startup or EndActivation, not application ingress.");
        var registration = _triggers?.Get(trigger.GetType()) ?? throw new TriggerNotValid("Trigger discovery has not been configured for this occurrence service.");
        var result = await ((IChronicleServicesAccessor)eventStore.Connection).Services.Occurrences.RecordOccurrence(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            Trigger = registration.Id,
            IdempotencyId = idempotencyId,
            Payload = JsonSerializer.Serialize(trigger, trigger.GetType(), _jsonOptions),
            Kind = Contracts.Storage.Occurrences.OccurrenceKind.Application
        }).EnsureSuccess();
        var status = result.Status switch
        {
            Contracts.Occurrences.OccurrenceIngressStatus.Recorded => OccurrenceRecordStatus.Recorded,
            Contracts.Occurrences.OccurrenceIngressStatus.AlreadyRecorded => OccurrenceRecordStatus.AlreadyRecorded,
            Contracts.Occurrences.OccurrenceIngressStatus.OccurrenceIdConflict => OccurrenceRecordStatus.OccurrenceIdConflict,
            Contracts.Occurrences.OccurrenceIngressStatus.OccurrencePayloadInvalid => OccurrenceRecordStatus.OccurrencePayloadInvalid,
            _ => throw new TriggerNotValid("The kernel returned an unknown occurrence ingress status.")
        };
        return new(result.Id, status);
    }

    /// <inheritdoc/>
    public async Task<EffectReceipt?> GetEffectReceipt(EffectKey key)
    {
        EffectReceiptSupport.Require(eventStore.Connection);
        EffectReceiptSupport.ToContract(key, null);
        var receipt = await ((IChronicleServicesAccessor)eventStore.Connection).Services.Occurrences.GetEffectReceipt(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            OccurrenceId = key.Occurrence,
            EffectKey = key.Name
        }).EnsureSuccess();

        // Query envelopes normalize an absent object to its empty wire shape.
        return receipt is null || string.IsNullOrEmpty(receipt.Id) ? null : EffectReceiptSupport.ToClient(key, receipt);
    }

    /// <inheritdoc/>
    public Task<EffectReceiptResult> RecordEffectReceipt(EffectKey key, EffectOutcome outcome, JsonNode? response = null)
    {
        ReadProtectionGuard.Append(nameof(RecordEffectReceipt));
        return RecordEffectReceiptCore(key, outcome, response);
    }

    /// <inheritdoc/>
    Task<EffectReceiptResult> IOccurrencesForUnitOfWork.RecordNoEvents(EffectKey key, JsonNode? response) =>
        RecordEffectReceiptCore(key, new NoEvents(), response);

    async Task<EffectReceiptResult> RecordEffectReceiptCore(EffectKey key, EffectOutcome outcome, JsonNode? response)
    {
        EffectReceiptSupport.Require(eventStore.Connection);
        var contract = EffectReceiptSupport.ToContract(key, response);
        var result = await ((IChronicleServicesAccessor)eventStore.Connection).Services.Occurrences.RecordEffectReceipt(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EffectReceipt = contract,
            Outcome = outcome switch
            {
                NoEvents => Contracts.Storage.Occurrences.EffectOutcome.NoEvents,
                Refused => Contracts.Storage.Occurrences.EffectOutcome.Refused,
                _ => throw new EffectReceiptNotValid("Receipt-only recording accepts NoEvents or Refused, never Appended.")
            },
            Reasons = outcome is Refused refused ? refused.Reasons.ToArray() : []
        }).EnsureSuccess();
        var status = result.Status switch
        {
            Contracts.Occurrences.EffectReceiptStatus.Recorded => EffectReceiptStatus.Recorded,
            Contracts.Occurrences.EffectReceiptStatus.AlreadyRecorded => EffectReceiptStatus.AlreadyRecorded,
            _ => throw new EffectReceiptNotValid("The kernel returned an unknown receipt status.")
        };

        return new(status, EffectReceiptSupport.ToClient(key, result.Receipt));
    }
}
