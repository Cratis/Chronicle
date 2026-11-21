// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_DormantObservers.when_asking_whether_an_observer_holds_a_client;

public class and_the_observer_is_resident_with_a_record_naming_the_client : given.dormant_observers
{
    void Establish()
    {
        _queues.IsDormant(_observer).Returns(false);
    }

    async Task Because() => _holds = await _dormantObservers.HoldsClient(_observer, _connection);

    [Fact] void should_not_hold_the_client_so_the_observer_is_asked_and_restores_a_lost_subscription() => _holds.ShouldBeFalse();
}
