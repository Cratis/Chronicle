// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections.Multiplexed;
using Cratis.Chronicle.Contracts.Observation.Multiplexed;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Services.Observation.Multiplexed;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Grpc.Server;

namespace Cratis.Chronicle.Server.for_MultiplexedObservation.given;

/// <summary>
/// Hosts the kernel's multiplexed observation over fake reactors and reducers on a real ASP.NET Core pipeline, and
/// reaches it with the client's <see cref="MultiplexedObservationChannel"/> over HTTP/2, so the contracts are
/// serialized on the wire exactly as between a client and a kernel.
/// </summary>
public class a_multiplexed_host : Specification
{
    protected static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    protected WebApplication _app;
    protected GrpcChannel _grpc;
    protected FakeReactors _reactors;
    protected FakeReducers _reducers;
    protected RecordingService _service;
    protected MultiplexedObservationChannel _channel;

    async Task Establish()
    {
        _reactors = new();
        _reducers = new();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddCodeFirstGrpc();
        builder.Services.AddSingleton<IReactors>(_reactors);
        builder.Services.AddSingleton<IReducers>(_reducers);
        builder.Services.AddSingleton<ILogger<MultiplexedObservation>>(NullLogger<MultiplexedObservation>.Instance);
        builder.Services.AddSingleton<MultiplexedObservation>();
        _app = builder.Build();
        _app.MapGrpcService<MultiplexedObservation>();
        await _app.StartAsync();

        var server = _app.GetTestServer();
        _grpc = GrpcChannel.ForAddress(server.BaseAddress, new GrpcChannelOptions { HttpHandler = server.CreateHandler() });
        _service = new(_grpc.CreateGrpcService<IMultiplexedObservation>());
        _channel = new(_service, NullLogger<MultiplexedObservationChannel>.Instance) { CoalesceDelay = TimeSpan.FromMilliseconds(50) };
    }

    async Task Destroy()
    {
        _grpc.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    protected OpenStream<ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve> OpenReactor(string id)
    {
        var stream = new OpenStream<ReactorMessage, global::Cratis.Chronicle.Contracts.Observation.EventsToObserve>();
        stream.Open(_channel.ObserveReactor(stream.Messages, CancellationToken.None));
        stream.Messages.OnNext(RegisterReactor(id));
        return stream;
    }

    protected OpenStream<ReducerMessage, ReduceOperationMessage> OpenReducer(string id)
    {
        var stream = new OpenStream<ReducerMessage, ReduceOperationMessage>();
        stream.Open(_channel.ObserveReducer(stream.Messages, CancellationToken.None));
        stream.Messages.OnNext(RegisterReducer(id));
        return stream;
    }

    protected Task WaitForReactors(int count) =>
        WaitUntil(() => _reactors.Calls.Count == count && _reactors.Calls.All(_ => _.Attached.IsCompleted && _.Received.Count >= 1));

    protected Task WaitForReducers(int count) =>
        WaitUntil(() => _reducers.Calls.Count == count && _reducers.Calls.All(_ => _.Attached.IsCompleted && _.Received.Count >= 1));

    protected static async Task WaitUntil(Func<bool> condition)
    {
        using var deadline = new CancellationTokenSource(_timeout);
        while (!condition())
        {
            if (deadline.IsCancellationRequested)
            {
                throw new TimeoutException("The condition was not met in time.");
            }

            await Task.Delay(20, deadline.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    protected static ReactorMessage RegisterReactor(string id) => new(new(new RegisterReactor
    {
        ConnectionId = "connection",
        EventStore = "store",
        Namespace = "namespace",
        Reactor = new global::Cratis.Chronicle.Contracts.Observation.Reactors.ReactorDefinition { ReactorId = id, EventSequenceId = "event-log" }
    }));

    protected static ReducerMessage RegisterReducer(string id) => new(new(new RegisterReducer
    {
        ConnectionId = "connection",
        EventStore = "store",
        Namespace = "namespace",
        Reducer = new global::Cratis.Chronicle.Contracts.Observation.Reducers.ReducerDefinition { ReducerId = id, EventSequenceId = "event-log" }
    }));

    protected static string ReactorIdOf(ReactorMessage message) => ((RegisterReactor)message.Content.Value!).Reactor.ReactorId;

    protected static global::Cratis.Chronicle.Contracts.Observation.EventsToObserve Events(string partition) => new() { Partition = partition };

    /// <summary>
    /// Counts the calls the channel makes and the messages each write carries.
    /// </summary>
    /// <param name="inner">The real gRPC proxy.</param>
    protected sealed class RecordingService(IMultiplexedObservation inner) : IMultiplexedObservation
    {
        readonly List<int> _writes = [];
        int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public IReadOnlyList<int> Writes
        {
            get
            {
                lock (_writes)
                {
                    return [.. _writes];
                }
            }
        }

        public IObservable<MultiplexedServerMessage> Observe(IObservable<MultiplexedClientMessage> messages, ProtoBuf.Grpc.CallContext context = default)
        {
            Interlocked.Increment(ref _calls);
            return inner.Observe(
                System.Reactive.Linq.Observable.Do(messages, write =>
                {
                    lock (_writes)
                    {
                        _writes.Add(write.Messages.Count);
                    }
                }),
                context);
        }
    }
}
