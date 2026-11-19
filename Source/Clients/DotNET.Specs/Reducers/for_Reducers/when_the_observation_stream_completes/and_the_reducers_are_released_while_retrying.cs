// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Reducers;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_the_observation_stream_completes;

public class and_the_reducers_are_released_while_retrying : given.a_reducer_whose_stream_completes
{
    int _attempts;

    protected override IObservable<ReduceOperationMessage> ReRegister()
    {
        _attempts++;
        if (_attempts == 2)
        {
            _reducers.Release();
        }

        throw new InvalidOperationException("kernel unavailable");
    }

    async Task Because()
    {
        await CompleteTheStreamAndRunTheCapturedWork();
    }

    [Fact] void should_stop_retrying_once_released() => _attempts.ShouldEqual(2);
}
