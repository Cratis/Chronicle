// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_the_observation_stream_completes;

public class and_re_registration_fails : given.a_reactor_whose_stream_completes
{
    Exception? _exception;

    protected override IObservable<EventsToObserve> ReRegister()
    {
        _registrationsAfterCompletion++;
        throw new InvalidOperationException("kernel unavailable");
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
    [Fact] void should_attempt_the_re_registration_once() => _registrationsAfterCompletion.ShouldEqual(1);
    [Fact] void should_log_an_error() => HasErrorLog().ShouldBeTrue();
}
