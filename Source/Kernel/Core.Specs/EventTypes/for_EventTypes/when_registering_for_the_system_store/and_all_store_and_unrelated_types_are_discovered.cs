// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_registering_for_the_system_store;

public class and_all_store_and_unrelated_types_are_discovered : given.an_event_types_with_multiple_stores
{
    Type[] _preparedForSystem;

    async Task Because()
    {
        await _eventTypes.DiscoverAndRegister(EventStoreName.System);
        _preparedForSystem = _preparations.Keys.ToArray();
        await _eventTypes.DiscoverAndRegister(_storeA);
    }

    [Fact] void should_only_prepare_the_system_type_for_system() => _preparedForSystem.ShouldEqual([typeof(AlertRaised)]);
    [Fact] async Task should_register_the_system_type_in_system() => await ShouldRegister(EventStoreName.System, typeof(AlertRaised));
    [Fact] async Task should_register_the_all_store_type_in_the_other_store() => await ShouldRegister(_storeA, typeof(WebhookAdded));
    [Fact] void should_only_register_one_type_in_system() => _schemas[EventStoreName.System].Count.ShouldEqual(1);
    [Fact] void should_only_register_one_type_in_the_other_store() => _schemas[_storeA].Count.ShouldEqual(1);
    [Fact] void should_not_prepare_unrelated_types() => _preparations.ContainsKey(typeof(object)).ShouldBeFalse();
}
