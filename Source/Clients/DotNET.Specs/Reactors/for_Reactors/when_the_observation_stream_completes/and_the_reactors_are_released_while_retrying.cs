// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_the_observation_stream_completes;

public class and_the_reactors_are_released_while_retrying : given.a_reactor_whose_stream_completes
{
    int _attempts;

    protected override IObservable<EventsToObserve> ReRegister()
    {
        _attempts++;
        if (_attempts == 2)
        {
            _reactors.Release();
        }

        throw new InvalidOperationException("kernel unavailable");
    }

    async Task Because()
    {
        await CompleteTheStreamAndRunTheCapturedWork();
    }

    [Fact] void should_stop_retrying_once_released() => _attempts.ShouldEqual(2);
}
