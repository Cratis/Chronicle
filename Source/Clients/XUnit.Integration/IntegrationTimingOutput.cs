// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Testcontainers streams successful kernel output here; no full container log is buffered.
/// </summary>
/// <param name="write">The writer receiving the timing records.</param>
internal sealed class IntegrationTimingOutput(Action<string> write) : Stream
{
    const int MaximumLineBytes = 16384;
#if NET8_0
    readonly object _lineLock = new();
#else
    readonly Lock _lineLock = new();
#endif
    readonly byte[] _line = new byte[MaximumLineBytes];
    int _length;
    bool _overflow;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationTimingOutput"/> class replaying records to the process outputs.
    /// </summary>
    internal IntegrationTimingOutput()
        : this(IntegrationTimings.Replay)
    {
    }

    /// <inheritdoc/>
    public override bool CanRead => false;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
    {
        lock (_lineLock)
        {
            for (var index = offset; index < offset + count; index++)
            {
                Append(buffer[index]);
            }
        }
    }

    /// <inheritdoc/>
    public override void Flush()
    {
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing) WarnIfUnfinished();
        base.Dispose(disposing);
    }

    static bool IsTiming(string line) =>
        line.StartsWith("CHRONICLE_INTEGRATION_TIMING ", StringComparison.Ordinal) ||
        line.StartsWith("CHRONICLE_INTEGRATION_SUMMARY ", StringComparison.Ordinal) ||
        line.StartsWith("CHRONICLE_INTEGRATION_TIMING_WARNING ", StringComparison.Ordinal);

    void Append(byte value)
    {
        if (value == (byte)'\n')
        {
            FinishLine();
            return;
        }
        if (_length < MaximumLineBytes)
        {
            _line[_length++] = value;
            return;
        }
        if (_overflow) return;
        _overflow = true;
        if (IsTiming(Encoding.UTF8.GetString(_line, 0, 64)))
        {
            write("CHRONICLE_INTEGRATION_TIMING_WARNING kernel timing line exceeded capture bound; report is incomplete");
        }
    }

    void FinishLine()
    {
        if (!_overflow)
        {
            var line = Encoding.UTF8.GetString(_line, 0, _length).TrimEnd('\r');
            if (IsTiming(line)) write(line);
        }
        _length = 0;
        _overflow = false;
    }

    void WarnIfUnfinished()
    {
        if (_length == 0) return;
        if (!IsTiming(Encoding.UTF8.GetString(_line, 0, _length))) return;
        write("CHRONICLE_INTEGRATION_TIMING_WARNING kernel output ended mid-line; report may be incomplete");
    }
}
