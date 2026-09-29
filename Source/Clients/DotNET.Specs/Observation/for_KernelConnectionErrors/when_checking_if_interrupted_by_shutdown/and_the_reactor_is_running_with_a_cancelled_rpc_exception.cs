// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Observation.for_KernelConnectionErrors.when_checking_if_interrupted_by_shutdown;

public class and_the_reactor_is_running_with_a_cancelled_rpc_exception : Specification
{
    bool _result;

    void Because() => _result = new RpcException(new Status(StatusCode.Cancelled, "gRPC call disposed")).IsInterruptedByShutdown(CancellationToken.None);

    [Fact] void should_not_be_interrupted_by_shutdown() => _result.ShouldBeFalse();
}
