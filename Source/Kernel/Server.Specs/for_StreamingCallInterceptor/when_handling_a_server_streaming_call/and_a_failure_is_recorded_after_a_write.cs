// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.when_handling_a_server_streaming_call;

public class and_a_failure_is_recorded_after_a_write : given.a_response_stream
{
    async Task Because() => _error = await Catch.Exception(() => _interceptor.ServerStreamingServerHandler<string, string>(
        "request", _writer, _context, async (_, writer, _) =>
        {
            await writer.WriteAsync("first");
            RecordFailure();
            await writer.WriteAsync("second");
        }));

    [Fact] void should_throw_the_recorded_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_keep_only_the_item_written_before_the_decision() => _writer.Items.ShouldEqual(["first"]);
}
