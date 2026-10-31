// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_Reactors.when_the_observation_stream_completes;

public class and_the_handler_is_cancelled_during_the_delay : given.a_reactor_whose_stream_completes
{
    protected override TimeSpan ReconnectDelay => Timeout.InfiniteTimeSpan;

    async Task Because()
    {
        await _reactors.Register();
        EndTheFirstStream();
        var work = _reconnectWork.Single()();
        _handler.Disconnect();
        await work.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_not_register_again() => _registrationsAfterCompletion.ShouldEqual(0);
    [Fact] void should_not_log_an_error() => HasErrorLog().ShouldBeFalse();
}
