// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.when_handling_a_server_streaming_call;

public class and_a_failure_is_recorded_during_a_write : given.a_response_stream
{
    async Task Because()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _writer.Completion = completion.Task;
        var call = Catch.Exception(() => _interceptor.ServerStreamingServerHandler<string, string>(
            "request", _writer, _context, (_, writer, _) => writer.WriteAsync("first")));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await _writer.WriteStarted.Task.WaitAsync(deadline.Token);
        RecordFailure();
        completion.SetResult();
        _error = await call.WaitAsync(deadline.Token);
    }

    [Fact] void should_throw_the_recorded_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_complete_the_write_started_before_the_decision() => _writer.Items.ShouldEqual(["first"]);
}
