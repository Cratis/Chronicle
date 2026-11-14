// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentTransport;

public class when_a_response_has_no_end : Specification
{
    const int Limit = 1024;

    EndlessStream _stream;
    byte[]? _body;
    byte[]? _fitting;
    byte[]? _declaredTooLong;

    void Establish() => _stream = new();

    async Task Because()
    {
        using var endless = new StreamContent(_stream);
        _body = await FederationEnrollmentTransport.ReadLimited(endless, Limit, CancellationToken.None);

        using var fitting = new ByteArrayContent(new byte[Limit]);
        _fitting = await FederationEnrollmentTransport.ReadLimited(fitting, Limit, CancellationToken.None);

        using var declared = new StreamContent(new EndlessStream());
        declared.Headers.ContentLength = Limit + 1;
        _declaredTooLong = await FederationEnrollmentTransport.ReadLimited(declared, Limit, CancellationToken.None);
    }

    [Fact] void should_give_up_without_a_body() => _body.ShouldBeNull();
    [Fact] void should_stop_reading_soon_after_the_limit() => (_stream.BytesRead <= Limit + 4096).ShouldBeTrue();
    [Fact] void should_accept_a_body_that_fits() => _fitting.Length.ShouldEqual(Limit);
    [Fact] void should_not_read_a_body_that_says_it_is_too_long() => _declaredTooLong.ShouldBeNull();

    sealed class EndlessStream : Stream
    {
        public long BytesRead { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            BytesRead += count;
            return count;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            BytesRead += buffer.Length;
            return ValueTask.FromResult(buffer.Length);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
