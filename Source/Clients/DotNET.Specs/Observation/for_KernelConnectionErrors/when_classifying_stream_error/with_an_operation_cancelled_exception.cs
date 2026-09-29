// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_KernelConnectionErrors.when_classifying_stream_error;

public class with_an_operation_cancelled_exception : Specification
{
    KernelConnectionErrorKind _result;

    void Because() => _result = new OperationCanceledException().ClassifyStreamError();

    [Fact] void should_be_cancelled() => _result.ShouldEqual(KernelConnectionErrorKind.Cancelled);
}
