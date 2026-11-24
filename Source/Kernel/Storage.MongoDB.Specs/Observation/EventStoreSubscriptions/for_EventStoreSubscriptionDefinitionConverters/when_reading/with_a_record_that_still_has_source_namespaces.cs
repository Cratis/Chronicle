// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionDefinitionConverters.when_reading;

public class with_a_record_that_still_has_source_namespaces : Specification
{
    BsonDocument _document;
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _result;

    void Establish()
    {
        _document = new EventStoreSubscriptionDefinition { Id = "subscription", SourceEventStore = "source", EventTypes = [new EventType("customer", 1)] }.ToBsonDocument();
        _document["SourceNamespaces"] = new BsonArray(["Customers"]);
    }

    void Because() => _result = BsonSerializer.Deserialize<EventStoreSubscriptionDefinition>(_document).ToKernel();

    [Fact] void should_still_carry_the_retired_element() => _document.Contains("SourceNamespaces").ShouldBeTrue();
    [Fact] void should_read_the_subscription() => _result.SourceEventStore.Value.ShouldEqual("source");
}
