// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestKit;

namespace Cratis.Chronicle.Grains.Observation.Jobs.for_CatchUpObserver.when_asking_if_can_resume;

public class and_it_is_subscribed : given.a_catchup_observer_and_a_request
{
    IObserver _observer;
    bool _result;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
        _observer.IsSubscribed().Returns(true);
    }

    async Task Because() => _result = await _job.WrappedCanResume();

    [Fact] void should_be_able_to_resume() => _result.ShouldBeTrue();
}
