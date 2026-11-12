// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.when_handling_a_server_streaming_call;

public class and_nothing_is_recorded : given.a_response_stream
{
    readonly WriteOptions _options = new(WriteFlags.NoCompress);
    WriteOptions? _observedOptions;
    ServerCallContext _observedContext;

    async Task Because() => _error = await Catch.Exception(() => _interceptor.ServerStreamingServerHandler<string, string>(
        "request", _writer, _context, async (_, writer, context) =>
        {
            _observedContext = context;
            writer.WriteOptions = _options;
            _observedOptions = writer.WriteOptions;
            await writer.WriteAsync("first");
            await writer.WriteAsync("second");
        }));

    [Fact] void should_complete_normally() => _error.ShouldBeNull();
    [Fact] void should_write_each_item() => _writer.Items.ShouldEqual(["first", "second"]);
    [Fact] void should_keep_the_call_context() => _observedContext.ShouldEqual(_context);
    [Fact] void should_set_the_inner_write_options() => _writer.WriteOptions.ShouldEqual(_options);
    [Fact] void should_get_the_inner_write_options() => _observedOptions.ShouldEqual(_options);
}
