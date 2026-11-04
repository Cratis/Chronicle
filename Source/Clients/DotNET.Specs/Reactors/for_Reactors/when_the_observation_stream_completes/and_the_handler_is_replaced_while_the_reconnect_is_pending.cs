// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_the_observation_stream_completes;

public class and_the_handler_is_replaced_while_the_reconnect_is_pending : given.a_reactor_whose_stream_completes
{
    async Task Because()
    {
        await _reactors.Register();
        EndTheFirstStream();
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
        foreach (var work in _reconnectWork.ToArray())
        {
            await work();
        }
    }

    [Fact] void should_have_replaced_the_handler() => ReferenceEquals(_reactors.GetHandlerById(_handler.Id), _handler).ShouldBeFalse();
    [Fact] void should_not_observe_again() => _registrationsAfterCompletion.ShouldEqual(0);
    [Fact] void should_not_log_an_error() => HasErrorLog().ShouldBeFalse();
}
