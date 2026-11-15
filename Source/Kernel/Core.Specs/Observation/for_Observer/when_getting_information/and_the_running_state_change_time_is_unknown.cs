// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_getting_information;

public class and_the_running_state_change_time_is_unknown : given.an_observer
{
    ObserverInformationSnapshot _result;

    async Task Establish()
    {
        _persistedRunningStateChangedAt = null;
        await Crash();
    }

    async Task Because() => _result = await _observer.GetInformation();

    [Fact] void should_leave_the_running_state_change_time_unknown() => _result.RunningStateChangedAt.ShouldBeNull();
}
