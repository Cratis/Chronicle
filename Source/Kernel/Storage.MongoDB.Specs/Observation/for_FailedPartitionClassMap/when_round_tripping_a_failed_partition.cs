// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_FailedPartitionClassMap;

public class when_round_tripping_a_failed_partition : Specification
{
    FailedPartition _restored;
    BsonDocument _document;
    DateTimeOffset _occurred;
    FailedPartitionId _id;

    void Establish()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(FailedPartition)))
        {
            BsonClassMap.RegisterClassMap<FailedPartition>(new FailedPartitionClassMap().Configure);
        }

        _occurred = new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
        var partition = new FailedPartition { Partition = "failed-partition" };
        _id = partition.Id;
        partition.AddAttempt(new FailedPartitionAttempt { SequenceNumber = 12UL, Occurred = _occurred });
        partition.AddAttempt(new FailedPartitionAttempt { SequenceNumber = 15UL, Occurred = _occurred.AddMinutes(1) });
        var bson = partition.ToBson();
        _document = BsonSerializer.Deserialize<BsonDocument>(bson);
        _restored = BsonSerializer.Deserialize<FailedPartition>(bson);
    }

    [Fact] void should_map_the_id() => _document.Contains("_id").ShouldBeTrue();
    [Fact] void should_not_serialize_the_derived_last_attempt() => _document.Contains(nameof(FailedPartition.LastAttempt)).ShouldBeFalse();
    [Fact] void should_restore_the_id() => _restored.Id.ShouldEqual(_id);
    [Fact] void should_restore_the_last_attempt_sequence_number() => _restored.LastAttempt.SequenceNumber.ShouldEqual((Cratis.Chronicle.Concepts.Events.EventSequenceNumber)15UL);
    [Fact] void should_restore_the_last_attempt_time() => _restored.LastAttempt.Occurred.ShouldEqual(_occurred.AddMinutes(1));
}
