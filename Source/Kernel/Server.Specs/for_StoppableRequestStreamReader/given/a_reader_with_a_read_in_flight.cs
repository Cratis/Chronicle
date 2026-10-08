// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Server.for_StoppableRequestStreamReader.given;

public class a_reader_with_a_read_in_flight : Specification
{
    protected IAsyncStreamReader<string> _requestStream;
    internal StoppableRequestStreamReader<string> _reader;
    protected TaskCompletionSource<bool> _pendingRead;
    protected CancellationToken _readCancellation;
    protected Task<bool> _readInFlight;

    protected virtual bool ReadEndsWhenCancelled => true;

    protected virtual CancellationToken CallerCancellation => CancellationToken.None;

    void Establish()
    {
        _pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _requestStream = Substitute.For<IAsyncStreamReader<string>>();
        _requestStream.MoveNext(Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            _readCancellation = callInfo.Arg<CancellationToken>();
            if (ReadEndsWhenCancelled)
            {
                _readCancellation.Register(() => _pendingRead.TrySetCanceled(_readCancellation));
            }

            return _pendingRead.Task;
        });
        _reader = new(_requestStream);
        _readInFlight = _reader.MoveNext(CallerCancellation);
    }

    void Destroy() => _reader.Dispose();
}
