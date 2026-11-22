// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_registering_for_multiple_event_stores;

public class and_one_store_mutates_its_schema : given.an_event_types_with_multiple_stores
{
    JsonSchema _canonical;
    string _original;

    void Establish()
    {
        _canonical = _eventTypes.GetJsonSchema(typeof(WebhookAdded));
        _original = _canonical.ToJson();
        _register = (store, schema) =>
        {
            if (store == _storeA)
            {
                schema.Node["properties"]!["details"]!["properties"]!["name"]!["type"] = "integer";
                schema.Node["properties"]!["details"]!["required"]!.AsArray().Add("extra");
            }
            return Task.FromResult(true);
        };
    }

    async Task Because()
    {
        await _eventTypes.DiscoverAndRegister(_storeA);
        await _eventTypes.DiscoverAndRegister(_storeB);
        _schemas[_storeA].First().Node["properties"]!["details"]!["required"]!.AsArray().Clear();
        _register = (_, _) => Task.FromResult(true);
        await _eventTypes.DiscoverAndRegister(_storeA);
    }

    [Fact] void should_preserve_the_canonical_schema() => _canonical.ToJson().ShouldEqual(_original);
    [Fact] void should_not_contaminate_the_other_store() => _schemas[_storeB].Single().ToJson().ShouldEqual(_original);
    [Fact] void should_register_a_clean_copy_on_retry() => _schemas[_storeA].Last().ToJson().ShouldEqual(_original);
    [Fact] void should_not_share_the_wrapper() => ReferenceEquals(_schemas[_storeA].First(), _canonical).ShouldBeFalse();
    [Fact] void should_not_share_the_root_node() => ReferenceEquals(_schemas[_storeB].Single().Node, _canonical.Node).ShouldBeFalse();
    [Fact] void should_not_share_nested_nodes() => ReferenceEquals(_schemas[_storeB].Single().Node["properties"]!["details"], _canonical.Node["properties"]!["details"]).ShouldBeFalse();
}
