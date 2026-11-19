// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_ed25519_signing : given.a_subscriber_with_signing
{
    static readonly ReplayEpoch _epoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));

    async Task Because()
    {
        await _subscriber.OnNext("partition", _events, new(null));
        await _subscriber.OnNext("partition", _events, new(null) { ReplayEpoch = _epoch });
        await _subscriber.OnNext("partition", [], new(null));
    }

    [Fact] void should_supply_a_signing_identity() => _identities.Take(2).All(identity => identity is not null).ShouldBeTrue();
    [Fact] void should_use_the_observer_store_not_the_payload() => _identities[0]!.EventStore.Value.ShouldEqual("store");
    [Fact] void should_include_the_namespace() => _identities[0]!.Namespace.Value.ShouldEqual("namespace");
    [Fact] void should_include_the_observer() => _identities[0]!.ObserverId.Value.ShouldEqual("webhook");
    [Fact] void should_include_the_event_sequence() => _identities[0]!.EventSequenceId.Value.ShouldEqual("log");
    [Fact] void should_identify_an_events_delivery() => _identities[0]!.Kind.ShouldEqual(WebhookMessageIdentity.EventsKind);
    [Fact] void should_use_the_materialized_batch_boundaries() => _identities[0]!.Batches.Single().ShouldEqual(new WebhookMessageBatch("partition", 42, 44));
    [Fact] void should_leave_live_delivery_without_a_replay_epoch() => _identities[0]!.ReplayEpoch.ShouldBeNull();
    [Fact] void should_forward_the_replay_epoch() => _identities[1]!.ReplayEpoch.ShouldEqual(_epoch);
    [Fact] void should_give_replays_a_different_id() => (WebhookMessageIds.For(_identities[0]!) != WebhookMessageIds.For(_identities[1]!)).ShouldBeTrue();
    [Fact] void should_leave_empty_deliveries_unsigned() => _identities[2].ShouldBeNull();
    [Fact] async Task should_read_the_committed_definition_once_per_delivery() => await _definitions.Received(3).Get("webhook");
}
