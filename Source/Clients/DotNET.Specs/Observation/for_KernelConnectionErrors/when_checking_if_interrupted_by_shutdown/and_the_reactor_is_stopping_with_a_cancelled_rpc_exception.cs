// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Observation.for_KernelConnectionErrors.when_checking_if_interrupted_by_shutdown;

public class and_the_reactor_is_stopping_with_a_cancelled_rpc_exception : Specification
{
    bool _result;

    void Because() => _result = new RpcException(new Status(StatusCode.Cancelled, "gRPC call disposed")).IsInterruptedByShutdown(new CancellationToken(canceled: true));

    [Fact] void should_be_interrupted_by_shutdown() => _result.ShouldBeTrue();
}
