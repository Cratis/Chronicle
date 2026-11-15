// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventStoreSubscriptions.for_EventStoreSubscriptionDefinitionConverters.when_round_tripping;

public class without_named_tags : Specification
{
    EventStoreSubscriptionDefinition _row;
    Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition _result;

    void Because()
    {
        _row = new Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition("subscription", "source", [new EventType("customer", 1)]).ToSql();
        _result = _row.ToKernel();
    }

    [Fact] void should_store_no_filter() => _row.NamedTags.ShouldBeNull();
    [Fact] void should_read_back_an_empty_filter() => _result.NamedTags.ShouldBeEmpty();
}
