// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_DormantObservers.when_asking_whether_an_observer_holds_a_client;

public class and_the_record_names_the_client_on_another_silo : given.dormant_observers
{
    void Establish()
    {
        _queues.IsDormant(_observer).Returns(true);
        _records.Names(_observer, _connection).Returns(false);
    }

    async Task Because() => _holds = await _dormantObservers.HoldsClient(_observer, _connection);

    [Fact] void should_not_hold_the_client() => _holds.ShouldBeFalse();
}
