// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventStoreSubscriptions.for_EventStoreSubscriptionDefinitionConverters.when_round_tripping;

public class with_named_tags : Specification
{
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _definition;
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _result;

    void Establish() => _definition = new("subscription", "source", [new EventType("customer", 1)])
    {
        NamedTags = [new NamedTag(new TagName("audience"), "lobby-a"), new NamedTag(new TagName("audience"), "lobby-b")]
    };

    void Because() => _result = _definition.ToSql().ToKernel();

    [Fact] void should_preserve_the_tag_filter() => _result.NamedTags.ShouldContainOnly(_definition.NamedTags);
}
