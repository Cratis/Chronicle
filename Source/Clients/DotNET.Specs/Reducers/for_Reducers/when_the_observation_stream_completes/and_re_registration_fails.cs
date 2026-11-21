// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation.Reducers;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_the_observation_stream_completes;

public class and_re_registration_fails : given.a_reducer_whose_stream_completes
{
    int _attempts;
    int _waits;
    Exception? _exception;

    protected override Func<TimeSpan, CancellationToken, Task> WaitBeforeReconnect => (_, _) =>
    {
        _waits++;
        return Task.CompletedTask;
    };

    protected override IObservable<ReduceOperationMessage> ReRegister()
    {
        _attempts++;
        if (_attempts == 1)
        {
            throw new InvalidOperationException("kernel unavailable");
        }

        _registrationsAfterCompletion++;
        return Observable.Never<ReduceOperationMessage>();
    }

    async Task Because()
    {
        try
        {
            await CompleteTheStreamAndRunTheCapturedWork();
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    [Fact] void should_not_throw_from_the_scheduled_work() => _exception.ShouldBeNull();
    [Fact] void should_try_again_after_the_failure() => _attempts.ShouldEqual(2);
    [Fact] void should_register_once_it_succeeds() => _registrationsAfterCompletion.ShouldEqual(1);
    [Fact] void should_wait_before_every_attempt() => _waits.ShouldEqual(2);
    [Fact] void should_log_an_error() => HasErrorLog().ShouldBeTrue();
}
