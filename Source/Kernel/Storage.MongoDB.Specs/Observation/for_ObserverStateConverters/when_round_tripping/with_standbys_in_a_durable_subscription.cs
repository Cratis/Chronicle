// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_standbys_in_a_durable_subscription : Specification
{
    KernelObserverState _restored;
    KernelObserverState _writtenBeforeStandbys;

    void Because()
    {
        var expected = new KernelObserverState
        {
            Identifier = "observer",
            DurableSubscription = new(
                ObserverSubscriptionKind.ClientReactor,
                false,
                null,
                [new("connection-1", "silo-a")],
                [new("connection-2", "silo-b", ["type-a+1+False", "type-b+2+True"], true, new(["tag"], "source", "stream"), true, false)])
        };
        _restored = BsonSerializer.Deserialize<ObserverState>(expected.ToMongoDB().ToBson()).ToKernel();

        // A document written before standbys were recorded has no such element at all.
        var document = new KernelObserverState
        {
            Identifier = "observer",
            DurableSubscription = new(ObserverSubscriptionKind.ClientReactor, false, null, [new("connection-1", "silo-a")])
        }.ToMongoDB().ToBsonDocument();
        var subscription = document["durableSubscription"].AsBsonDocument;
        foreach (var name in subscription.Names.Where(name => name.Equals("standbys", StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            subscription.Remove(name);
        }

        _writtenBeforeStandbys = BsonSerializer.Deserialize<ObserverState>(document.ToBson()).ToKernel();
    }

    [Fact] void should_keep_the_standby() => _restored.DurableSubscription!.Standbys!.Select(standby => $"{standby.ConnectionId}@{standby.Silo}").ShouldContainOnly("connection-2@silo-b");
    [Fact] void should_keep_the_event_types_of_the_standby() => string.Join(',', _restored.DurableSubscription!.Standbys![0].EventTypes).ShouldEqual("type-a+1+False,type-b+2+True");
    [Fact] void should_keep_the_filters_of_the_standby() => _restored.DurableSubscription!.Standbys![0].Filters!.EventStreamType.ShouldEqual("stream");
    [Fact] void should_keep_the_generation_delivery_of_the_standby() => _restored.DurableSubscription!.Standbys![0].PinsGenerations.ShouldBeTrue();
    [Fact] void should_keep_the_targets_of_a_record_written_before_standbys() => _writtenBeforeStandbys.DurableSubscription!.Targets.Select(target => target.ConnectionId).ShouldContainOnly("connection-1");
    [Fact] void should_read_a_record_written_before_standbys_as_having_none() => (_writtenBeforeStandbys.DurableSubscription!.Standbys ?? []).ShouldBeEmpty();
}
