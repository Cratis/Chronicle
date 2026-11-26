// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_DormantObservers;

public class and_the_queue_holds_it_as_dormant : given.dormant_observers
{
    void Establish()
    {
        _queues.IsDormant(_observer).Returns(true);
    }

    async Task Because() => _result = await _dormantObservers.Find(_observer);

    [Fact] void should_return_its_durable_subscription() => _result.ShouldEqual(_record);
}
