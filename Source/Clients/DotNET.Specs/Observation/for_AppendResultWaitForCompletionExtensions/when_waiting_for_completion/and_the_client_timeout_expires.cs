// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Observation.for_AppendResultWaitForCompletionExtensions.when_waiting_for_completion;

public class and_the_client_timeout_expires : given.an_append_result_for_completion
{
    void Establish()
    {
        _observers.WaitForCompletion(Arg.Any<Contracts.Observation.WaitForObserverCompletionRequest>(), Arg.Any<CallContext>())
            .Returns(call => WaitForCancellation(call.ArgAt<CallContext>(1).CancellationToken));
    }

    async Task Because() => _result = await _appendResult.WaitForCompletion(TimeSpan.FromMilliseconds(1));

    static async Task<Contracts.Observation.WaitForObserverCompletionResponse> WaitForCancellation(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "cancelled"));
        }

        return new();
    }

    [Fact] void should_report_a_timeout() => _result.TimedOut.ShouldBeTrue();
    [Fact] void should_not_report_an_observer_failure() => _result.FailedPartitions.ShouldBeEmpty();
}
