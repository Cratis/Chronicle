// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery.when_deriving_an_effect_key;

public class and_the_event_is_delivered_again : given.a_delivery
{
    IdempotencyKey _first;
    IdempotencyKey _recoveredOrReplayed;

    void Because()
    {
        _first = _delivery.IdempotencyKeyFor("charge");
        _recoveredOrReplayed = ReactorDelivery.For(
            _reactor,
            ContextFor(EventStore, Namespace, _partition, _sequenceNumber) with { ObservationState = EventObservationState.Replay }).IdempotencyKeyFor("charge");
    }

    [Fact] void should_derive_the_same_key() => _recoveredOrReplayed.ShouldEqual(_first);
    [Fact] void should_derive_a_key_within_the_limit() => _first.Value.Length.ShouldEqual(64);
}
