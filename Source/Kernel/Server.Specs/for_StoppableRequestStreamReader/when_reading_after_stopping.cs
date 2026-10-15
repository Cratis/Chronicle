// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Server.for_StoppableRequestStreamReader;

public class when_reading_after_stopping : Specification
{
    IAsyncStreamReader<string> _requestStream;
    StoppableRequestStreamReader<string> _reader;
    bool _result;

    async Task Establish()
    {
        _requestStream = Substitute.For<IAsyncStreamReader<string>>();
        _reader = new(_requestStream);
        await _reader.Stop();
    }

    async Task Because() => _result = await _reader.MoveNext(CancellationToken.None);

    [Fact] void should_report_the_end_of_the_stream() => _result.ShouldBeFalse();
    [Fact] void should_not_read_from_the_request_stream() => _requestStream.DidNotReceive().MoveNext(Arg.Any<CancellationToken>());

    void Destroy() => _reader.Dispose();
}
