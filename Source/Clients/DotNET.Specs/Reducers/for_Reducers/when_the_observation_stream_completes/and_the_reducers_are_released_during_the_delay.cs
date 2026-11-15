// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers.for_Reducers.when_the_observation_stream_completes;

public class and_the_reducers_are_released_during_the_delay : given.a_reducer_whose_stream_completes
{
    protected override Func<TimeSpan, CancellationToken, Task> WaitBeforeReconnect => (_, _) =>
    {
        _reducers.Release();
        return Task.CompletedTask;
    };

    async Task Because() => await CompleteTheStreamAndRunTheCapturedWork();

    [Fact] void should_not_observe_again() => _registrationsAfterCompletion.ShouldEqual(0);
    [Fact] void should_not_log_an_error() => HasErrorLog().ShouldBeFalse();
}
