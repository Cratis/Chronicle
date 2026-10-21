// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Observation.for_KernelConnectionErrors.when_classifying_stream_error;

public class with_a_client_is_not_connected_rpc_exception : Specification
{
    KernelConnectionErrorKind _result;

    void Because() => _result = new RpcException(new Status(StatusCode.Unknown, "Exception was thrown by handler. ClientIsNotConnected: Client 'abc' is not connected.")).ClassifyStreamError();

    [Fact] void should_be_session_lost() => _result.ShouldEqual(KernelConnectionErrorKind.SessionLost);
}
