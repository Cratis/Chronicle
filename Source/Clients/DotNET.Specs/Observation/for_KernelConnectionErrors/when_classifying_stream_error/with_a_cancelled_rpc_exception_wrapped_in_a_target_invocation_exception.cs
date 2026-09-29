// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Grpc.Core;

namespace Cratis.Chronicle.Observation.for_KernelConnectionErrors.when_classifying_stream_error;

public class with_a_cancelled_rpc_exception_wrapped_in_a_target_invocation_exception : Specification
{
    KernelConnectionErrorKind _result;

    void Because() => _result = new TargetInvocationException(new RpcException(new Status(StatusCode.Cancelled, "gRPC call disposed"))).ClassifyStreamError();

    [Fact] void should_be_cancelled() => _result.ShouldEqual(KernelConnectionErrorKind.Cancelled);
}
