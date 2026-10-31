// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_getting_state;

public class and_the_document_carries_an_unknown_field : Specification
{
    EventSequenceState _result;

    void Because() => _result = BsonSerializer.Deserialize<EventSequenceState>(new BsonDocument
    {
        { "sequenceNumber", 42L },
        { "tailSequenceNumberPerEventType", new BsonDocument() },
        { "aFutureCoverageField", new BsonArray { "email" } }
    });

    [Fact] void should_ignore_the_unknown_field_using_the_registered_conventions() => _result.SequenceNumber.Value.ShouldEqual(42UL);
}
