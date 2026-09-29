// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Observation.for_KernelConnectionErrors.when_classifying_handler_error;

public class with_an_aggregate_exception_holding_a_genuine_failure : Specification
{
    KernelConnectionErrorKind _result;

    void Because() => _result = new AggregateException(new RpcException(new Status(StatusCode.Unavailable, "Unavailable")), new InvalidOperationException("Something broke")).ClassifyHandlerError();

    [Fact] void should_be_a_failure() => _result.ShouldEqual(KernelConnectionErrorKind.Failure);
}
