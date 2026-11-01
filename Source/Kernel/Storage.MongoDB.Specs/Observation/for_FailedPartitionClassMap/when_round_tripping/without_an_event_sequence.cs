// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_FailedPartitionClassMap.when_round_tripping;

public class without_an_event_sequence : Specification
{
    FailedPartition _result;
    BsonDocument _document;

    void Establish()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(FailedPartition)))
        {
            BsonClassMap.RegisterClassMap<FailedPartition>(new FailedPartitionClassMap().Configure);
        }
    }

    void Because()
    {
        _document = new FailedPartition().ToBsonDocument();
        _result = BsonSerializer.Deserialize<FailedPartition>(_document);
    }

    [Fact] void should_not_write_a_sequence_element() => _document.Contains("eventSequenceId").ShouldBeFalse();
    [Fact] void should_leave_legacy_failures_untagged() => _result.EventSequenceId.ShouldBeNull();
}
