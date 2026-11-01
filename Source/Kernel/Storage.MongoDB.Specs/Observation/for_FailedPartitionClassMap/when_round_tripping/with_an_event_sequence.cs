// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_FailedPartitionClassMap.when_round_tripping;

public class with_an_event_sequence : Specification
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
        _document = new FailedPartition { EventSequenceId = new("A") }.ToBsonDocument();
        _result = BsonSerializer.Deserialize<FailedPartition>(_document);
    }

    [Fact] void should_store_the_sequence_as_a_string() => _document["eventSequenceId"].AsString.ShouldEqual("A");
    [Fact] void should_keep_the_sequence() => _result.EventSequenceId.ShouldEqual(new EventSequenceId("A"));
}
