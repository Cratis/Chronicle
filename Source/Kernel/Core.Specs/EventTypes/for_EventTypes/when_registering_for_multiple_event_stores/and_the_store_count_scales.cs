// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.EventTypes.for_EventTypes.when_registering_for_multiple_event_stores;

public class and_the_store_count_scales : given.an_event_types_with_multiple_stores
{
    readonly List<int> _preparationCounts = [];
    readonly List<int> _registrationCounts = [];

    async Task Because()
    {
        await _eventTypes.DiscoverAndRegister(EventStoreName.System);
        for (var index = 1; index <= 10; index++)
        {
            EventStoreName store = $"scaled-store-{index}";
            AddStore(store);
            await _eventTypes.DiscoverAndRegister(store);
            if (index is 1 or 2 or 10)
            {
                _preparationCounts.Add(_preparations.Values.Sum());
                _registrationCounts.Add(_schemas.Values.Sum(schemas => schemas.Count));
            }
        }
    }

    [Fact] void should_keep_preparations_constant_for_one_two_and_ten_stores() => _preparationCounts.ShouldEqual([2, 2, 2]);
    [Fact] void should_register_in_every_store() => _registrationCounts.ShouldEqual([2, 3, 11]);
    [Fact] void should_prepare_the_system_type_once() => _preparations[typeof(AlertRaised)].ShouldEqual(1);
    [Fact] void should_prepare_the_all_store_type_once() => _preparations[typeof(WebhookAdded)].ShouldEqual(1);
}
