// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Multiplexed;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.Multiplexed;

/// <summary>
/// Carries the observation of every reactor and reducer of one connection over a single kernel stream.
/// </summary>
/// <remarks>
/// <para>
/// A reactor or reducer opens a stream the way it opens a per-observer call: it hands over its message observable and
/// gets the observable of what the kernel sends. The stream is a tagged slice of the shared call to the kernel, which
/// is opened when the first stream opens and closed when the last one has. Messages queued at the same time travel
/// as one write, so registering many observers at start-up, or again after a reconnect, is one write instead of one
/// call each.
/// </para>
/// <para>
/// When the shared call fails or ends, every stream on it fails or ends with it, the same way every per-observer call
/// of a lost connection does, and the next stream that opens starts a fresh shared call.
/// </para>
/// </remarks>
/// <param name="service">The <see cref="IMultiplexedObservation"/> of the kernel.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
public sealed class MultiplexedObservationChannel(IMultiplexedObservation service, ILogger<MultiplexedObservationChannel> logger)
{
    readonly Lock _lock = new();
    SharedCall? _current;
    ulong _nextStreamId;

    /// <summary>
    /// Gets how long the first queued message waits for companions before a write goes out. Messages that arrive in
    /// that time share the write.
    /// </summary>
    public TimeSpan CoalesceDelay { get; init; } = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// Gets the most messages one write carries.
    /// </summary>
    public int MaxMessagesPerWrite { get; init; } = 500;

    /// <summary>
    /// Opens a reactor stream.
    /// </summary>
    /// <param name="messages">The messages of the reactor, the first being its registration.</param>
    /// <param name="cancellationToken">Ends the stream when cancelled.</param>
    /// <returns>An observable of what the kernel sends the reactor. The stream opens when it is subscribed to.</returns>
    public IObservable<EventsToObserve> ObserveReactor(IObservable<ReactorMessage> messages, CancellationToken cancellationToken) =>
        Open(
            MultiplexedObserverKind.Reactor,
            messages,
            (message, first) => new() { Kind = first ? MultiplexedObserverKind.Reactor : MultiplexedObserverKind.Unspecified, Reactor = message },
            server => server.Reactor,
            cancellationToken);

    /// <summary>
    /// Opens a reducer stream.
    /// </summary>
    /// <param name="messages">The messages of the reducer, the first being its registration.</param>
    /// <param name="cancellationToken">Ends the stream when cancelled.</param>
    /// <returns>An observable of what the kernel sends the reducer. The stream opens when it is subscribed to.</returns>
    public IObservable<ReduceOperationMessage> ObserveReducer(IObservable<ReducerMessage> messages, CancellationToken cancellationToken) =>
        Open(
            MultiplexedObserverKind.Reducer,
            messages,
            (message, first) => new() { Kind = first ? MultiplexedObserverKind.Reducer : MultiplexedObserverKind.Unspecified, Reducer = message },
            server => server.Reducer,
            cancellationToken);

    IObservable<TOut> Open<TIn, TOut>(
        MultiplexedObserverKind kind,
        IObservable<TIn> messages,
        Func<TIn, bool, MultiplexedStreamMessage> toStreamMessage,
        Func<MultiplexedServerMessage, TOut?> payload,
        CancellationToken cancellationToken)
        where TOut : class =>
        Observable.Create<TOut>(observer =>
        {
            var stream = new ClientStream(
                observer.OnCompleted,
                observer.OnError,
                server =>
                {
                    if (payload(server) is { } value)
                    {
                        observer.OnNext(value);
                    }
                });
            SharedCall call;
            ulong streamId;
            lock (_lock)
            {
                _current ??= new SharedCall(this, service, logger);
                call = _current;
                streamId = ++_nextStreamId;
                call.Add(streamId, stream);
            }

            logger.OpeningStream(streamId, kind);
            var first = true;
            var closed = 0;
            void Close()
            {
                if (Interlocked.Exchange(ref closed, 1) == 0)
                {
                    call.Close(streamId);
                }
            }

            var subscription = messages.Subscribe(
                message =>
                {
                    call.Send(streamId, toStreamMessage(message, first));
                    first = false;
                },
                observer.OnError,
                Close);
            var cancellation = cancellationToken.Register(() =>
            {
                Close();
                observer.OnError(new OperationCanceledException(cancellationToken));
            });
            return Disposable.Create(() =>
            {
                cancellation.Dispose();
                subscription.Dispose();
                Close();
            });
        });

    void Release(SharedCall call)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_current, call))
            {
                _current = null;
            }
        }
    }

    /// <summary>
    /// The receiving end of one stream. Everything the stream's observer is told goes through its own queue and
    /// pump, in order, so an observer that is slow to take a message holds up its own stream and no other.
    /// </summary>
    sealed class ClientStream
    {
        readonly Action _complete;
        readonly Action<Exception> _fail;
        readonly Action<MultiplexedServerMessage> _deliver;
        readonly System.Threading.Channels.Channel<Action> _queue = System.Threading.Channels.Channel.CreateUnbounded<Action>(new() { SingleReader = true });
        ulong _sequence;

        public ClientStream(Action complete, Action<Exception> fail, Action<MultiplexedServerMessage> deliver)
        {
            _complete = complete;
            _fail = fail;
            _deliver = deliver;
            _ = Task.Run(Pump);
        }

        public bool Accept(MultiplexedServerMessage message)
        {
            if (message.Sequence != _sequence + 1)
            {
                return false;
            }

            _sequence = message.Sequence;
            return true;
        }

        public void Deliver(MultiplexedServerMessage message) => _queue.Writer.TryWrite(() => _deliver(message));

        public void Complete()
        {
            _queue.Writer.TryWrite(_complete);
            _queue.Writer.TryComplete();
        }

        public void Fail(Exception error)
        {
            _queue.Writer.TryWrite(() => _fail(error));
            _queue.Writer.TryComplete();
        }

        async Task Pump()
        {
            await foreach (var action in _queue.Reader.ReadAllAsync())
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    _queue.Writer.TryComplete();
                    _fail(ex);
                    return;
                }
            }
        }
    }

    sealed class SharedCall
    {
        readonly MultiplexedObservationChannel _channel;
        readonly ILogger<MultiplexedObservationChannel> _logger;
        readonly Lock _lock = new();
        readonly Dictionary<ulong, ClientStream> _streams = [];
        readonly System.Threading.Channels.Channel<MultiplexedStreamMessage> _outbound = System.Threading.Channels.Channel.CreateUnbounded<MultiplexedStreamMessage>(new() { SingleReader = true });
        readonly IDisposable? _subscription;
        bool _ended;

        public SharedCall(MultiplexedObservationChannel channel, IMultiplexedObservation service, ILogger<MultiplexedObservationChannel> logger)
        {
            _channel = channel;
            _logger = logger;
            try
            {
                var subscription = service.Observe(Writes()).Subscribe(Receive, End, () => End(null));
                bool ended;
                lock (_lock)
                {
                    ended = _ended;
                    _subscription = subscription;
                }

                if (ended)
                {
                    subscription.Dispose();
                }
            }
            catch (Exception ex)
            {
                End(ex);
            }
        }

        public void Add(ulong streamId, ClientStream stream)
        {
            lock (_lock)
            {
                if (_ended)
                {
                    // The call ended between the lookup and now; the stream sees what every stream of it saw.
                    stream.Fail(new InvalidOperationException("The multiplexed observation connection ended before the stream opened."));
                    return;
                }

                _streams[streamId] = stream;
            }
        }

        public void Send(ulong streamId, MultiplexedStreamMessage message)
        {
            message.StreamId = streamId;
            _outbound.Writer.TryWrite(message);
        }

        public void Close(ulong streamId)
        {
            bool last;
            lock (_lock)
            {
                if (!_streams.Remove(streamId))
                {
                    return;
                }

                last = _streams.Count == 0;
            }

            _outbound.Writer.TryWrite(new() { StreamId = streamId, Close = true });
            if (last)
            {
                End(null);
            }
        }

        IObservable<MultiplexedClientMessage> Writes() =>
            Observable.Create<MultiplexedClientMessage>(async (observer, cancellationToken) =>
            {
                var reader = _outbound.Reader;
                while (await reader.WaitToReadAsync(cancellationToken))
                {
                    if (_channel.CoalesceDelay > TimeSpan.Zero)
                    {
                        await Task.Delay(_channel.CoalesceDelay, cancellationToken);
                    }

                    var batch = new List<MultiplexedStreamMessage>();
                    while (batch.Count < _channel.MaxMessagesPerWrite && reader.TryRead(out var message))
                    {
                        batch.Add(message);
                    }

                    if (batch.Count > 0)
                    {
                        _logger.WritingMessages(batch.Count);
                        observer.OnNext(new() { Messages = batch });
                    }
                }
            });

        void Receive(MultiplexedServerMessage message)
        {
            ClientStream? stream;
            lock (_lock)
            {
                _streams.TryGetValue(message.StreamId, out stream);
            }

            if (stream is null)
            {
                return;
            }

            if (!stream.Accept(message))
            {
                _logger.OutOfOrderMessage(message.StreamId, message.Sequence);
                End(new InvalidOperationException($"The multiplexed observation stream {message.StreamId} delivered message {message.Sequence} out of order."));
                return;
            }

            if (message.End is null)
            {
                stream.Deliver(message);
                return;
            }

            lock (_lock)
            {
                _streams.Remove(message.StreamId);
            }

            if (message.End.StatusCode == 0)
            {
                stream.Complete();
            }
            else
            {
                stream.Fail(new Grpc.Core.RpcException(new((Grpc.Core.StatusCode)message.End.StatusCode, message.End.Detail)));
            }
        }

        void End(Exception? error)
        {
            ClientStream[] streams;
            lock (_lock)
            {
                if (_ended)
                {
                    return;
                }

                _ended = true;
                streams = [.. _streams.Values];
                _streams.Clear();
            }

            _channel.Release(this);
            _outbound.Writer.TryComplete();
            _subscription?.Dispose();
            _logger.ConnectionEnded(streams.Length, error);
            foreach (var stream in streams)
            {
                if (error is null)
                {
                    stream.Complete();
                }
                else
                {
                    stream.Fail(error);
                }
            }
        }
    }
}
