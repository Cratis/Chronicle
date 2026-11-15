// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionDefinitionConverters.when_round_tripping;

public class with_named_tags : Specification
{
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _definition;
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _result;

    void Establish() => _definition = new("subscription", "source", [new EventType("customer", 1)])
    {
        NamedTags = [new NamedTag(new TagName("audience"), "lobby-a")]
    };

    void Because() => _result = BsonSerializer.Deserialize<EventStoreSubscriptionDefinition>(_definition.ToMongoDB().ToBsonDocument()).ToKernel();

    [Fact] void should_preserve_the_tag_filter() => _result.NamedTags.ShouldContainOnly(_definition.NamedTags);
}
