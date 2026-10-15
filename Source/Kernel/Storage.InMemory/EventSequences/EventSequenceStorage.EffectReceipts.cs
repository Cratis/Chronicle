// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

public partial class EventSequenceStorage : IEffectReceiptStorage
{
    readonly Occurrences.EffectReceiptState _effectReceiptState = effectReceipts ?? new();

    /// <inheritdoc/>
    public Task<EffectReceipt?> GetEffectReceipt(string id)
    {
        lock (_effectReceiptState.Sync)
        {
            var receipt = _effectReceiptState.Receipts.GetValueOrDefault(id);
            return Task.FromResult(receipt is null ? null : receipt with { Reasons = receipt.Reasons.ToArray() });
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EffectReceipt>> GetEffectReceipts(OccurrenceId occurrenceId)
    {
        lock (_effectReceiptState.Sync)
        {
            return Task.FromResult<IEnumerable<EffectReceipt>>(_effectReceiptState.Receipts.Values.Where(receipt => receipt.Lease.OccurrenceId == occurrenceId).Select(receipt => receipt with { Reasons = receipt.Reasons.ToArray() }).ToArray());
        }
    }

    /// <inheritdoc/>
    public async Task<EffectReceiptWriteResult> AppendManyWithEffectReceipt(IEnumerable<EventToAppendToStorage> events, EffectReceipt receipt)
    {
        if (await GetEffectReceipt(receipt.Id) is not null)
        {
            return EffectReceiptWriteResult.AlreadyRecorded;
        }

        var batch = events.ToArray();
        var identities = new List<Identity>();
        foreach (var item in batch)
        {
            identities.Add(await identityStorage.GetFor(item.CausedByChain));
        }

        // The namespace-wide lock prevents the same effect being written to two different sequences.
        lock (_effectReceiptState.Sync)
        {
            lock (_lock)
            {
                if (_effectReceiptState.Receipts.ContainsKey(receipt.Id))
                {
                    return EffectReceiptWriteResult.AlreadyRecorded;
                }

                EffectReceiptValidation.Validate(batch, receipt, eventSequenceId);
                var numbers = new HashSet<Concepts.Events.EventSequenceNumber>();
                if (batch.Any(item => !numbers.Add(item.SequenceNumber) || _events.Exists(existing => existing.Context.SequenceNumber == item.SequenceNumber)))
                {
                    return EffectReceiptWriteResult.DuplicateEventSequenceNumber;
                }

                // Construct every event before mutating either map, including serialization that can fail.
                var appended = batch.Select((item, index) => BuildAppendedEvent(item.SequenceNumber, item.EventSourceType, item.EventSourceId, item.EventStreamType, item.EventStreamId, item.EventType, item.CorrelationId, item.Causation, identities[index], item.Tags, item.Occurred, item.GenerationalContent, item.ContentHashes, item.Subject, item.NamedTags, item.EventSource)).ToArray();
                for (var index = 0; index < batch.Length; index++)
                {
                    var item = batch[index];
                    _events.Add(appended[index]);
                    _originalCausedByChains[item.SequenceNumber] = item.CausedByChain.ToArray();
                    TrackMetadata(appended[index], _originalCausedByChains[item.SequenceNumber]);
                    _appendedGenerations[item.SequenceNumber] = item.EventType.Generation.Value;
                    _generationHashes[item.SequenceNumber] = new Dictionary<Concepts.Events.EventTypeGeneration, Concepts.Events.EventHash>(item.ContentHashes);
                }

                _effectReceiptState.Receipts.Add(receipt.Id, receipt with { Reasons = receipt.Reasons.ToArray() });
                return EffectReceiptWriteResult.Written;
            }
        }
    }
}
