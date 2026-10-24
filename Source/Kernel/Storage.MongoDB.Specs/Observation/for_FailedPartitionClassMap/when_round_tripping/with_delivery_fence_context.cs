// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_FailedPartitionClassMap.when_round_tripping;

public class with_delivery_fence_context : Specification
{
    DeliveryFenceContext _expected;
    FailedPartition _restored;

    void Establish()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(FailedPartition)))
        {
            BsonClassMap.RegisterClassMap<FailedPartition>(new FailedPartitionClassMap().Configure);
        }

        IReadOnlyList<DeliveryFenceTarget> targets =
        [
            new("reactor-subscriber", "original-key-a", "connection-a", "127.0.0.1:11111@1"),
            new("reactor-subscriber", "original-key-b", "connection-b", "127.0.0.1:22222@2")
        ];
        _expected = new(new DateTimeOffset(2026, 10, 20, 23, 0, 0, TimeSpan.Zero), targets);
    }

    void Because()
    {
        var partition = new FailedPartition
        {
            Partition = "partition",
            Attempts =
            [
                new() { SequenceNumber = new(42), Kind = FailureKind.Timeout, DeliveryFence = _expected },
                new() { SequenceNumber = new(43), Kind = FailureKind.Timeout, DeliveryFence = null }
            ]
        };
        _restored = BsonSerializer.Deserialize<FailedPartition>(partition.ToBson());
    }

    [Fact] void should_keep_both_attempts() => _restored.Attempts.Count().ShouldEqual(2);
    [Fact] void should_keep_the_original_deadline() => _restored.Attempts.First().DeliveryFence!.ReleaseBy.ShouldEqual(_expected.ReleaseBy);
    [Fact] void should_keep_the_original_targets() => _restored.Attempts.First().DeliveryFence!.Targets.ShouldContainOnly(_expected.Targets);
    [Fact] void should_keep_a_null_delivery_fence() => _restored.LastAttempt.DeliveryFence.ShouldBeNull();
    [Fact] void should_keep_the_timeout_kinds() => _restored.Attempts.All(attempt => attempt.Kind == FailureKind.Timeout).ShouldBeTrue();
}
