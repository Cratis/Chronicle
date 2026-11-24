// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation.Multiplexed;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Multiplexed;

/// <summary>
/// Holds the writer of one multiplexed connection and the streams open on it, and fans messages in and out.
/// </summary>
/// <remarks>
/// <para>
/// A stream is one call of <see cref="IReactors.Observe"/> or <see cref="IReducers.Observe"/>: the stream's client
/// messages feed its input and what it emits is tagged with the stream identifier and written to the connection.
/// Writes are serialized, and the sequence of a stream is assigned inside the same lock as the write, so the order
/// a client sees for one stream is the order the observer produced it in.
/// </para>
/// <para>
/// A stream that ends, completes or fails is reported with a <see cref="MultiplexedStreamEnd"/> and leaves the
/// others untouched. A connection that ends closes every stream it holds the way a closed per-observer call does.
/// </para>
/// </remarks>
/// <param name="reactors">The <see cref="IReactors"/> serving reactor streams.</param>
/// <param name="reducers">The <see cref="IReducers"/> serving reducer streams.</param>
/// <param name="output">The writer of the connection.</param>
/// <param name="context">The <see cref="CallContext"/> of the connection, which identifies the caller of every stream on it.</param>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
internal sealed class MultiplexedConnection(
    IReactors reactors,
    IReducers reducers,
    IObserver<MultiplexedServerMessage> output,
    CallContext context,
    ILogger<MultiplexedObservation> logger) : IDisposable
{
    readonly Lock _lock = new();
    readonly Dictionary<ulong, OpenStream> _streams = [];
    bool _disposed;
    bool _finished;

    /// <summary>
    /// Gets the number of streams currently open.
    /// </summary>
    public int OpenStreams
    {
        get
        {
            lock (_lock)
            {
                return _streams.Count;
            }
        }
    }

    /// <summary>
    /// Handles a write from the client.
    /// </summary>
    /// <param name="message">The <see cref="MultiplexedClientMessage"/>.</param>
    public void Receive(MultiplexedClientMessage message)
    {
        foreach (var streamMessage in message.Messages)
        {
            Receive(streamMessage);
        }
    }

    /// <summary>
    /// Handles the failure of the connection's request stream.
    /// </summary>
    /// <param name="error">The failure.</param>
    public void Fail(Exception error)
    {
        if (Finish())
        {
            output.OnError(error);
        }
    }

    /// <summary>
    /// Handles the completion of the connection's request stream or its cancellation.
    /// </summary>
    public void Complete()
    {
        if (Finish())
        {
            output.OnCompleted();
        }
    }

    /// <inheritdoc/>
    public void Dispose() => Finish();

    static (int Code, string Detail) Describe(Exception? error) => error switch
    {
        null => (0, string.Empty),
        RpcException rpc => ((int)rpc.StatusCode, rpc.Status.Detail),
        OperationCanceledException => ((int)StatusCode.Cancelled, string.Empty),
        _ => ((int)StatusCode.Unknown, error.Message)
    };

    void Receive(MultiplexedStreamMessage message)
    {
        if (message.Close)
        {
            Close(message.StreamId);
            return;
        }

        OpenStream? stream;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            if (!_streams.TryGetValue(message.StreamId, out stream))
            {
                stream = TryOpen(message);
                if (stream is null)
                {
                    return;
                }
            }
            else
            {
                stream.Push(message);
                return;
            }
        }

        stream.Start(message);
    }

    OpenStream? TryOpen(MultiplexedStreamMessage message)
    {
        switch (message.Kind)
        {
            case MultiplexedObserverKind.Reactor when message.Reactor is not null:
#pragma warning disable CA2000 // Disposed when the stream closes, which is the end of its lifetime.
                var reactorMessages = new Subject<ReactorMessage>();
#pragma warning restore CA2000
                var reactorStream = new OpenStream(
                    message.StreamId,
                    message.Kind,
                    first => reactorMessages.OnNext(first.Reactor!),
                    next =>
                    {
                        if (next.Reactor is not null)
                        {
                            reactorMessages.OnNext(next.Reactor);
                        }
                    },
                    () =>
                    {
                        reactorMessages.OnCompleted();
                        reactorMessages.Dispose();
                    });
                _streams[message.StreamId] = reactorStream;
                logger.StreamOpened(message.StreamId, message.Kind);
                reactorStream.Connect(reactors.Observe(reactorMessages, context).Subscribe(
                    events => Write(reactorStream, new() { Reactor = events }),
                    error => End(reactorStream, error),
                    () => End(reactorStream, null)));
                return reactorStream;
            case MultiplexedObserverKind.Reducer when message.Reducer is not null:
#pragma warning disable CA2000 // Disposed when the stream closes, which is the end of its lifetime.
                var reducerMessages = new Subject<ReducerMessage>();
#pragma warning restore CA2000
                var reducerStream = new OpenStream(
                    message.StreamId,
                    message.Kind,
                    first => reducerMessages.OnNext(first.Reducer!),
                    next =>
                    {
                        if (next.Reducer is not null)
                        {
                            reducerMessages.OnNext(next.Reducer);
                        }
                    },
                    () =>
                    {
                        reducerMessages.OnCompleted();
                        reducerMessages.Dispose();
                    });
                _streams[message.StreamId] = reducerStream;
                logger.StreamOpened(message.StreamId, message.Kind);
                reducerStream.Connect(reducers.Observe(reducerMessages, context).Subscribe(
                    operation => Write(reducerStream, new() { Reducer = operation }),
                    error => End(reducerStream, error),
                    () => End(reducerStream, null)));
                return reducerStream;
            default:
                logger.UnknownStreamMessage(message.StreamId, message.Kind);
                return null;
        }
    }

    void Close(ulong streamId)
    {
        OpenStream? stream;
        lock (_lock)
        {
            if (!_streams.Remove(streamId, out stream))
            {
                return;
            }
        }

        logger.StreamClosedByClient(streamId);
        stream.Close();
    }

    void Write(OpenStream stream, MultiplexedServerMessage message)
    {
        lock (_lock)
        {
            if (_finished || stream.Ended)
            {
                return;
            }

            message.StreamId = stream.Id;
            message.Sequence = ++stream.Sequence;
            output.OnNext(message);
        }
    }

    void End(OpenStream stream, Exception? error)
    {
        lock (_lock)
        {
            if (_finished || stream.Ended)
            {
                return;
            }

            stream.Ended = true;
            _streams.Remove(stream.Id);
            var (code, detail) = Describe(error);
            logger.StreamEnded(stream.Id, code);
            output.OnNext(new()
            {
                StreamId = stream.Id,
                Sequence = ++stream.Sequence,
                End = new() { StatusCode = code, Detail = detail }
            });
        }

        stream.Close();
    }

    bool Finish()
    {
        OpenStream[] open;
        lock (_lock)
        {
            if (_finished)
            {
                return false;
            }

            _finished = true;
            _disposed = true;
            open = [.. _streams.Values];
            _streams.Clear();
        }

        foreach (var stream in open)
        {
            stream.Close();
        }

        return true;
    }

    sealed class OpenStream(ulong id, MultiplexedObserverKind kind, Action<MultiplexedStreamMessage> start, Action<MultiplexedStreamMessage> push, Action complete)
    {
        IDisposable? _subscription;

        public ulong Id { get; } = id;

        public MultiplexedObserverKind Kind { get; } = kind;

        public ulong Sequence { get; set; }

        public bool Ended { get; set; }

        public void Connect(IDisposable subscription) => _subscription = subscription;

        public void Start(MultiplexedStreamMessage first) => start(first);

        public void Push(MultiplexedStreamMessage message) => push(message);

        public void Close()
        {
            complete();
            _subscription?.Dispose();
        }
    }
}
