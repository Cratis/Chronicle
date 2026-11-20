// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_DormantObservers;

public class and_it_is_resident : given.dormant_observers
{
    void Establish()
    {
        _queues.IsDormant(_observer).Returns(false);
    }

    async Task Because() => _result = await _dormantObservers.Find(_observer);

    [Fact] void should_not_report_it_as_dormant() => _result.ShouldBeNull();
}
