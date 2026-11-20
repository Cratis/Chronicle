// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_the_observation_stream_completes;

public class and_re_registration_keeps_failing_until_the_handler_is_cancelled : given.a_reducer_whose_stream_completes
{
    int _attempts;

    protected override IObservable<ReduceOperationMessage> ReRegister()
    {
        _attempts++;
        if (_attempts == 3)
        {
            _handlerCancellation.Cancel();
        }

        throw new InvalidOperationException("kernel unavailable");
    }

    async Task Because()
    {
        await CompleteTheStreamAndRunTheCapturedWork();
    }

    [Fact] void should_keep_trying_until_the_handler_is_cancelled() => _attempts.ShouldEqual(3);
}
