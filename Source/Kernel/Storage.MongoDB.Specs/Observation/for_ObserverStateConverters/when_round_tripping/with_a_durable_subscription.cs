// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_a_durable_subscription : Specification
{
    KernelObserverState _restored;
    KernelObserverState _legacy;

    void Because()
    {
        var expected = new KernelObserverState
        {
            Identifier = "observer",
            DurableSubscription = new(
                ObserverSubscriptionKind.ClientReducer,
                true,
                new(["tag-a", "tag-b"], "source", "stream"),
                [new("connection-1", "silo-a"), new("connection-2", "silo-b")])
        };
        _restored = BsonSerializer.Deserialize<ObserverState>(expected.ToMongoDB().ToBson()).ToKernel();
        _legacy = BsonSerializer.Deserialize<ObserverState>(new KernelObserverState { Identifier = "legacy" }.ToMongoDB().ToBson()).ToKernel();
    }

    [Fact] void should_keep_the_kind() => _restored.DurableSubscription.Kind.ShouldEqual(ObserverSubscriptionKind.ClientReducer);
    [Fact] void should_keep_the_page_delivery() => _restored.DurableSubscription.DeliversPages.ShouldBeTrue();
    [Fact] void should_keep_the_filters() => string.Join(',', _restored.DurableSubscription.Filters.Tags).ShouldEqual("tag-a,tag-b");
    [Fact] void should_keep_the_filter_stream_type() => _restored.DurableSubscription.Filters.EventStreamType.ShouldEqual("stream");
    [Fact] void should_keep_the_targets() => string.Join(',', _restored.DurableSubscription.Targets.Select(target => $"{target.ConnectionId}@{target.Silo}")).ShouldEqual("connection-1@silo-a,connection-2@silo-b");
    [Fact] void should_not_invent_a_subscription_for_a_legacy_document() => _legacy.DurableSubscription.ShouldBeNull();
}
