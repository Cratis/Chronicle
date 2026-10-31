// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using System.Text;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookConnection.given;

public class http_response_stream : Stream
{
    readonly Pipe _responses = new();
    readonly StringBuilder _request = new();
    readonly Stream _reader;

    public http_response_stream() => _reader = _responses.Reader.AsStream();

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public override int Read(byte[] buffer, int offset, int count) => _reader.Read(buffer, offset, count);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => _reader.ReadAsync(buffer, cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        _request.Append(Encoding.ASCII.GetString(buffer.Span));
        if (!_request.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal)) return;
        _request.Clear();
        await _responses.Writer.WriteAsync("HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\n[]"u8.ToArray(), cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _responses.Writer.Complete();
            _reader.Dispose();
        }
        base.Dispose(disposing);
    }
}
