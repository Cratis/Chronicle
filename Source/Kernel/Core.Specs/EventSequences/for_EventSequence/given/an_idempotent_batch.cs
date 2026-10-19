// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences.Idempotency;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_idempotent_batch : an_event_sequence_with_idempotency_keys
{
    protected List<IdempotencyKey?> _batchKeys = [new("one"), new("two"), new("three")];
    protected IdempotentAppendManyResult _batchResult;
    protected ConcurrencyScopes _batchScopes = new(new Dictionary<EventSourceId, ConcurrencyScope>());

    protected Task<IdempotentAppendManyResult> AppendBatch() => _eventSequence.AppendManyWithIdempotencyKeys(_events, _batchKeys, CorrelationId.New(), [], Identity.System, _batchScopes);

    protected void DurableAt(int index, EventSequenceNumber number)
    {
        var input = Input(_events[index]) with { Identity = _batchKeys[index]!.Identity };
        _durable[_batchKeys[index]!.Identity] = new(number, AppendFingerprint.Compute(input, []).AsT0.Format());
        if (_stateStorage.State.SequenceNumber <= number) _stateStorage.State.SequenceNumber = number.Next();
    }

    protected async Task ProtectBatch(bool erased = false)
    {
        await Protect(erased);
        _events[0] = _request;
        _complianceManager.LocateProtectedValues(Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(call =>
            call.Arg<JsonObject>()["name"] is { } name
                ? [new ProtectedValueLocation("name", ProtectionKind.Pii, _eventSourceId.Value, name.DeepClone())]
                : []);
    }

    protected void CheckedScopes() => _batchScopes = new(_events.ToDictionary(@event => @event.EventSourceId, _ => new ConcurrencyScope(EventSequenceNumber.First, true, null, null, null, null)));
}
