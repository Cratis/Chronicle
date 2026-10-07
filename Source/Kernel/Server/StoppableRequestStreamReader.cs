// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Represents an <see cref="IAsyncStreamReader{T}"/> whose reading can be stopped, waiting for a read in flight to finish.
/// </summary>
/// <param name="inner">The <see cref="IAsyncStreamReader{T}"/> of the call's request stream.</param>
/// <typeparam name="T">Type of message in the request stream.</typeparam>
/// <remarks>
/// protobuf-net.Grpc hands a request stream to a service as an <see cref="IObservable{T}"/> and reads it in the background
/// without any cancellation, so a read is still pending when the service ends the call. Kestrel then completes the request
/// body and returns the HTTP/2 stream to its pool with that read outstanding. When the read resumes it runs against the next
/// call on the same connection, which fails with "Reading is already in progress" or loses a message to the earlier call
/// (https://github.com/Cratis/Chronicle/issues/4537). Stopping cancels the pending read and waits for it, so nothing reads
/// the request stream once the call has ended.
/// </remarks>
internal sealed class StoppableRequestStreamReader<T>(IAsyncStreamReader<T> inner) : IAsyncStreamReader<T>, IDisposable
{
    readonly CancellationTokenSource _stopped = new();
    readonly Lock _lock = new();
    Task _read = Task.CompletedTask;
    bool _isStopped;

    /// <inheritdoc/>
    public T Current => inner.Current;

    /// <inheritdoc/>
    public Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_isStopped)
            {
                return Task.FromResult(false);
            }

            var read = Read(cancellationToken);
            _read = read;
            return read;
        }
    }

    /// <summary>
    /// Stop reading the request stream and wait for a read in flight to finish.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The outcome of the read in flight is not observed here: the call is over, so whatever it read or failed with
    /// belongs to nobody. Every later <see cref="MoveNext"/> reports the end of the stream without reading.
    /// </remarks>
    public async Task Stop()
    {
        Task read;
        lock (_lock)
        {
            _isStopped = true;
            read = _read;
        }

        await _stopped.CancelAsync();
        await read.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    /// <inheritdoc/>
    public void Dispose() => _stopped.Dispose();

    async Task<bool> Read(CancellationToken cancellationToken)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopped.Token);
        return await inner.MoveNext(cancellation.Token);
    }
}
