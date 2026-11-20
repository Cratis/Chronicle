// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_the_observation_stream_completes;

public class and_re_registration_times_out : given.a_reactor_whose_stream_completes
{
    int _attempts;

    protected override Func<TimeSpan, CancellationToken, Task> WaitBeforeReconnect => (_, _) => Task.CompletedTask;

    protected override IObservable<EventsToObserve> ReRegister()
    {
        _attempts++;
        if (_attempts == 1)
        {
            throw new TaskCanceledException("connecting timed out");
        }

        _registrationsAfterCompletion++;
        return Observable.Never<EventsToObserve>();
    }

    Task Because() => CompleteTheStreamAndRunTheCapturedWork();

    [Fact] void should_try_again_after_the_timeout() => _attempts.ShouldEqual(2);
    [Fact] void should_register_once_it_succeeds() => _registrationsAfterCompletion.ShouldEqual(1);
}
