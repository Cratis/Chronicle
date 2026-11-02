// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Cratis.Chronicle.Storage;
using Cratis.Traces;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.given;

public class a_registration : Specification
{
    protected IGrainFactory _grainFactory;
    protected IReactor _client;
    protected IObserverRegistrationGate _gate;
    protected IDisposable _lease;
    protected readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _entering = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource<IDisposable> _permission = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource<Exception> _error = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<IDisposable> _connections = [];
    Reactors _reactors;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _client = Substitute.For<IReactor>();
        _grainFactory.GetGrain<IReactor>(Arg.Any<string>()).Returns(_client);
        _gate = Substitute.For<IObserverRegistrationGate>();
        _lease = Substitute.For<IDisposable>();
        _lease.When(_ => _.Dispose()).Do(_ => _released.TrySetResult());
        SubstituteExtensions.Returns(_gate.Enter(Arg.Any<CancellationToken>()), _ => new ValueTask<IDisposable>(_lease));
        _reactors = new(
            _grainFactory,
            Substitute.For<IReactorMediator>(),
            Substitute.For<IStorage>(),
            new JsonSerializerOptions(),
            new ActivitySource<Reactors>(),
            Options.Create(new ChronicleOptions()),
            NullLogger<Reactors>.Instance,
            _gate);
        _grainFactory.ClearReceivedCalls();
        _client.ClearReceivedCalls();
        _gate.ClearReceivedCalls();
    }

    protected void ExhaustGate() => SubstituteExtensions.Returns(_gate.Enter(Arg.Any<CancellationToken>()), call => WaitForPermit(call.Arg<CancellationToken>()));

    protected void Connect(string connectionId = "connection", CancellationToken cancellationToken = default)
    {
        var messages = new Subject<ReactorMessage>();
        _connections.Add(messages);
        _connections.Add(_reactors.Observe(messages, new CallContext(new CallOptions(cancellationToken: cancellationToken))).Subscribe(_ => { }, error => _error.TrySetResult(error)));
        messages.OnNext(new(new(new RegisterReactor
        {
            ConnectionId = connectionId,
            EventStore = "store",
            Namespace = "namespace",
            Reactor = new Contracts.Observation.Reactors.ReactorDefinition { ReactorId = "reactor", EventSequenceId = "event-log" }
        })));
    }

    protected void Disconnect() => _connections[^1].Dispose();

    async ValueTask<IDisposable> WaitForPermit(CancellationToken cancellationToken)
    {
        _entering.TrySetResult();
        try
        {
            return await _permission.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _cancelled.TrySetResult();
            throw;
        }
    }

    void Destroy()
    {
        foreach (var connection in _connections.AsEnumerable().Reverse())
        {
            connection.Dispose();
        }
    }
}
