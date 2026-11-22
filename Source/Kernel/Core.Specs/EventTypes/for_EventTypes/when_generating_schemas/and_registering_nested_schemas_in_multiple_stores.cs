// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_generating_schemas;

public class and_registering_nested_schemas_in_multiple_stores : given.an_event_types_with_multiple_stores
{
    string _expected;

    void Establish()
    {
        _eventTypes = new(_types, _storage, NullLogger<EventTypes>.Instance);
        _expected = _eventTypes.GetJsonSchema(typeof(WebhookAdded)).ToJson();
    }

    async Task Because()
    {
        await _eventTypes.DiscoverAndRegister(_storeA);
        _schemas[_storeA].Single().Node["properties"]!["eventTypes"]!["items"]!["properties"]!["id"]!["type"] = "integer";
        await _eventTypes.DiscoverAndRegister(_storeB);
    }

    [Fact] void should_preserve_the_real_exported_schema() => _eventTypes.GetJsonSchema(typeof(WebhookAdded)).ToJson().ShouldEqual(_expected);
    [Fact] void should_register_the_original_shape_in_the_other_store() => _schemas[_storeB].Single().ToJson().ShouldEqual(_expected);
    [Fact] void should_keep_the_mutated_registration_separate() => _schemas[_storeA].Single().ToJson().ShouldNotEqual(_expected);
    [Fact] void should_keep_the_nested_item_nodes_independent() => ReferenceEquals(_schemas[_storeA].Single().Node["properties"]!["eventTypes"]!["items"], _schemas[_storeB].Single().Node["properties"]!["eventTypes"]!["items"]).ShouldBeFalse();
}
