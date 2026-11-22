// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_registering_after_schema_resolution;

public class and_the_schema_is_already_cached : given.an_event_types_with_multiple_stores
{
    JsonSchema _canonical;

    void Establish()
    {
        _eventTypes.GetClrTypeFor(typeof(WebhookAdded).GetEventType().Id);
        _canonical = _eventTypes.GetJsonSchema(typeof(WebhookAdded));
    }

    async Task Because() => await _eventTypes.DiscoverAndRegister(_storeA);

    [Fact] void should_not_prepare_the_schema_again() => _preparations[typeof(WebhookAdded)].ShouldEqual(1);
    [Fact] void should_preserve_the_canonical_schema() => ReferenceEquals(_canonical, _eventTypes.GetJsonSchema(typeof(WebhookAdded))).ShouldBeTrue();
    [Fact] void should_preserve_clr_resolution() => _eventTypes.GetClrTypeFor(typeof(WebhookAdded).GetEventType().Id).ShouldEqual(typeof(WebhookAdded));
    [Fact] void should_register_equal_json() => _schemas[_storeA].Single().ToJson().ShouldEqual(_canonical.ToJson());
    [Fact] void should_register_an_independent_schema() => ReferenceEquals(_schemas[_storeA].Single(), _canonical).ShouldBeFalse();
}
