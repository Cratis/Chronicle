// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Observation.for_AppendResultWaitForCompletionExtensions.when_waiting_for_completion;

public class and_the_timeout_is_infinite : given.an_append_result_for_completion
{
    Contracts.Observation.WaitForObserverCompletionRequest _request = null!;
    CancellationToken _cancellationToken;

    void Establish()
    {
        _observers.WaitForCompletion(Arg.Do<Contracts.Observation.WaitForObserverCompletionRequest>(request => _request = request), Arg.Do<CallContext>(context => _cancellationToken = context.CancellationToken))
            .Returns(async _ =>
            {
                // This double is intentionally slow enough to expose an accidental finite client deadline.
                await Task.Delay(TimeSpan.FromMilliseconds(250));
                return new Contracts.Observation.WaitForObserverCompletionResponse { IsSuccess = true };
            });
    }

    async Task Because() => _result = await _appendResult.WaitForCompletion(Timeout.InfiniteTimeSpan);

    [Fact] void should_not_set_a_server_timeout() => _request.TimeoutMilliseconds.ShouldEqual(0L);
    [Fact] void should_not_cancel_the_client_call() => _cancellationToken.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_complete() => _result.IsSuccess.ShouldBeTrue();
}
