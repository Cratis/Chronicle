// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.when_handling_a_server_streaming_call;

public class and_a_failure_is_recorded_before_a_write : given.a_response_stream
{
    async Task Because() => _error = await Catch.Exception(() => _interceptor.ServerStreamingServerHandler<string, string>(
        "request", _writer, _context, async (_, writer, _) =>
        {
            RecordFailure();
            await writer.WriteAsync("item");
        }));

    [Fact] void should_throw_the_recorded_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_write_nothing() => _writer.Items.ShouldBeEmpty();
}
