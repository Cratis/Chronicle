// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Observation.for_KernelConnectionErrors.when_classifying_stream_error;

public class with_an_unavailable_rpc_exception_wrapped_in_another_exception : Specification
{
    KernelConnectionErrorKind _result;

    void Because() => _result = new InvalidOperationException("Wrapped", new RpcException(new Status(StatusCode.Unavailable, "Error connecting to subchannel."))).ClassifyStreamError();

    [Fact] void should_be_connection_lost() => _result.ShouldEqual(KernelConnectionErrorKind.ConnectionLost);
}
