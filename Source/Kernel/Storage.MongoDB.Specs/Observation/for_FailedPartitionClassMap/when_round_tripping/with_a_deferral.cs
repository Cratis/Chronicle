// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_FailedPartitionClassMap.when_round_tripping;

public class with_a_deferral : Specification
{
    PartitionDeferral _expected;
    FailedPartition _restored;

    void Establish()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(FailedPartition)))
        {
            BsonClassMap.RegisterClassMap<FailedPartition>(new FailedPartitionClassMap().Configure);
        }

        var since = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        _expected = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, since, since.AddMinutes(3), new(42));
    }

    void Because() => _restored = BsonSerializer.Deserialize<FailedPartition>(new FailedPartition { Deferral = _expected }.ToBson());

    [Fact] void should_keep_the_whole_deferral() => _restored.Deferral.ShouldEqual(_expected);
    [Fact] void should_not_invent_attempts() => _restored.Attempts.ShouldBeEmpty();
}
