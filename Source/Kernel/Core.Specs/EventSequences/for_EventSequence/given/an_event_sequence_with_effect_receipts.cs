// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

using EffectReceipt = Cratis.Chronicle.Occurrences.EffectReceipt;
using StoredEffectReceipt = Cratis.Chronicle.Storage.Occurrences.EffectReceipt;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_effect_receipts : an_event_sequence
{
    protected EffectReceipt _receipt = new("occurrence", "command", 1, "{\"answer\":42}");
    protected Storage.InMemory.EventSequences.EventSequenceStorage _durableStorage;
    protected IEffectReceiptStorage _receiptStorage;
    protected bool _commitThenFail;
    protected EffectReceiptWriteResult? _collision;
    protected AppendManyResult _result;

    /// <summary>
    /// Gets the receipt identity.
    /// </summary>
    protected string ReceiptId => OccurrenceIdentities.ForEffect(new(_receipt.OccurrenceId), _receipt.EffectKey);

    /// <summary>
    /// Gets an event for the effect.
    /// </summary>
    protected EventToAppend AnEvent => new(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, [], new JsonObject());

    /// <inheritdoc/>
    protected override async Task<EventSequence> CreateEventSequence()
    {
        _durableStorage = new(EventStore, EventStoreNamespace, SequenceId, _identityStorage);
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage, IEffectReceiptStorage>();
        _receiptStorage = (IEffectReceiptStorage)_eventSequenceStorage;
        _namespaceStorage.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(_eventSequenceStorage);
        _eventSequenceStorage.GetEventAt(Arg.Any<EventSequenceNumber>()).Returns(call => _durableStorage.GetEventAt(call.Arg<EventSequenceNumber>()));
        _receiptStorage.GetEffectReceipt(Arg.Any<string>()).Returns(call => _durableStorage.GetEffectReceipt(call.Arg<string>()));
        _receiptStorage.AppendManyWithEffectReceipt(Arg.Any<IEnumerable<EventToAppendToStorage>>(), Arg.Any<StoredEffectReceipt>())
            .Returns(async call =>
            {
                if (_collision is { } collision) return collision;
                var result = await _durableStorage.AppendManyWithEffectReceipt(call.Arg<IEnumerable<EventToAppendToStorage>>(), call.Arg<StoredEffectReceipt>());
                if (_commitThenFail)
                {
                    _commitThenFail = false;
                    throw new SimulatedStateWriteError();
                }

                return result;
            });

        return await base.CreateEventSequence();
    }

    /// <summary>
    /// Appends the enrolled effect.
    /// </summary>
    /// <param name="events">The events, or one default event.</param>
    /// <param name="scopes">The concurrency scopes.</param>
    /// <returns>The append result.</returns>
    protected Task<AppendManyResult> AppendEffect(IEnumerable<EventToAppend>? events = null, ConcurrencyScopes? scopes = null) =>
        _eventSequence.AppendManyWithEffectReceipt(events ?? [AnEvent], CorrelationId.New(), [], Identity.System, scopes ?? new(new Dictionary<EventSourceId, ConcurrencyScope>()), _receipt);
}
