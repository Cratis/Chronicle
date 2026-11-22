// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_registering_for_multiple_event_stores;

public class and_a_store_is_registered_again : given.an_event_types_with_multiple_stores
{
    JsonSchema _canonical;

    async Task Because()
    {
        await _eventTypes.DiscoverAndRegister(_storeA);
        _canonical = _eventTypes.GetJsonSchema(typeof(WebhookAdded));
        await _eventTypes.DiscoverAndRegister(_storeB);
        await _eventTypes.DiscoverAndRegister(_storeA);
    }

    [Fact] void should_prepare_the_all_store_type_once() => _preparations[typeof(WebhookAdded)].ShouldEqual(1);
    [Fact] async Task should_register_twice_in_the_first_store() => await ShouldRegister(_storeA, typeof(WebhookAdded), 2);
    [Fact] async Task should_register_in_the_second_store() => await ShouldRegister(_storeB, typeof(WebhookAdded));
    [Fact] void should_keep_the_canonical_schema() => ReferenceEquals(_canonical, _eventTypes.GetJsonSchema(typeof(WebhookAdded))).ShouldBeTrue();
    [Fact] void should_give_each_registration_its_own_schema() => _schemas.Values.SelectMany(schemas => schemas).Distinct().Count().ShouldEqual(3);
    [Fact] void should_copy_again_for_the_same_store() => ReferenceEquals(_schemas[_storeA].First(), _schemas[_storeA].Last()).ShouldBeFalse();
}
