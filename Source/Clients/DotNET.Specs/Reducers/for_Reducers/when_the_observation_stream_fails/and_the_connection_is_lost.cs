// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_the_observation_stream_fails;

public class and_the_connection_is_lost : given.a_reducer_whose_stream_completes
{
    async Task Because() =>
        await FailTheStreamAndRunTheCapturedWork(new RpcException(new Status(StatusCode.Unavailable, "gone")));

    [Fact] void should_schedule_a_re_registration() => _reconnectWork.Count.ShouldEqual(1);
    [Fact] void should_register_again() => _registrationsAfterCompletion.ShouldEqual(1);
    [Fact] void should_not_request_a_reconnect_of_the_whole_client() => _eventStore.Connection.DidNotReceive().RequestReconnect();
    [Fact] void should_not_log_an_error() => HasErrorLog().ShouldBeFalse();
}
