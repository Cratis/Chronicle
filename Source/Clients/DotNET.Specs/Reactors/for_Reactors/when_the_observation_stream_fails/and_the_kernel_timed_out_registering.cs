// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_the_observation_stream_fails;

/// <summary>
/// The kernel gives up subscribing a reactor whose observer stays busy and fails the stream. The reactor must be
/// registered again, with backoff, and not stay disconnected until someone restarts the client.
/// </summary>
public class and_the_kernel_timed_out_registering : given.a_reactor_whose_stream_completes
{
    async Task Because()
    {
        await _reactors.Register();
        FailTheFirstStream(new RpcException(new Status(StatusCode.Unknown, "TimeoutException: Response did not arrive on time in 00:00:30")));
        foreach (var work in _reconnectWork.ToArray())
        {
            await work();
        }
    }

    [Fact] void should_schedule_a_re_registration() => _reconnectWork.Count.ShouldEqual(1);
    [Fact] void should_register_again() => _registrationsAfterCompletion.ShouldEqual(1);
    [Fact] void should_not_request_a_reconnect_of_the_whole_client() => _eventStore.Connection.DidNotReceive().RequestReconnect();
    [Fact] void should_log_an_error() => HasErrorLog().ShouldBeTrue();
}
