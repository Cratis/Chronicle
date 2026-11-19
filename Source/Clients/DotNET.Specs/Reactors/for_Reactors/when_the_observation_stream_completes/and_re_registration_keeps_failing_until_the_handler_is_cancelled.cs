// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_the_observation_stream_completes;

public class and_re_registration_keeps_failing_until_the_handler_is_cancelled : given.a_reactor_whose_stream_completes
{
    int _attempts;

    protected override IObservable<EventsToObserve> ReRegister()
    {
        _attempts++;
        if (_attempts == 3)
        {
            _handler.Disconnect();
        }

        throw new InvalidOperationException("kernel unavailable");
    }

    async Task Because()
    {
        await CompleteTheStreamAndRunTheCapturedWork();
    }

    [Fact] void should_keep_trying_until_the_handler_is_cancelled() => _attempts.ShouldEqual(3);
}
