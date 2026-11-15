// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMessageIds;

public class when_building_from_events : given.an_identity
{
    WebhookMessageIdentity _result;
    WebhookMessageIdentity _empty;

    void Because()
    {
        var key = new ObserverKey("observer-id", "event-store", "namespace", new EventSequenceId("sequence"));
        _result = WebhookMessageIdentity.ForEvents(
            key,
            (Key)"partition-1",
            [AppendedEvent.EmptyWithEventSequenceNumber(42), AppendedEvent.EmptyWithEventSequenceNumber(43)],
            _epoch);
        _empty = WebhookMessageIdentity.ForEvents(key, (Key)"partition-1", [], null);
    }

    [Fact] void should_use_the_first_and_last_sequence_numbers() => WebhookMessageIds.For(_result).ShouldEqual(WebhookMessageIds.For(Create(epoch: _epoch)));
    [Fact] void should_be_an_events_message() => _result.Kind.ShouldEqual(WebhookMessageIdentity.EventsKind);
    [Fact] void should_use_unavailable_for_an_empty_batch() => _empty.Batches.Single().First.ShouldEqual(EventSequenceNumber.Unavailable);
}
