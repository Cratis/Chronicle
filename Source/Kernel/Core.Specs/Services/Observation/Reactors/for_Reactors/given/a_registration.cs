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
    protected Subject<ReactorMessage> _messages;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _client = Substitute.For<IReactor>();
        _grainFactory.GetGrain<IReactor>(Arg.Any<string>()).Returns(_client);
        _gate = Substitute.For<IObserverRegistrationGate>();
        _lease = Substitute.For<IDisposable>();
        _lease.When(_ => _.Dispose()).Do(_ => _released.TrySetResult());
#pragma warning disable CA2012 // Configuring the substitute; the ValueTask is the stubbed call, not an operation to await
        _gate.Enter(Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<IDisposable>(_lease));
#pragma warning restore CA2012
        _reactors = new(
            _grainFactory,
            CreateMediator(),
            Substitute.For<IStorage>(),
            new JsonSerializerOptions(),
            new ActivitySource<Reactors>(),
            Options.Create(new ChronicleOptions()),
            AllowingConnectionOwnership.Create(),
            NullLogger<Reactors>.Instance,
            _gate,
            NamespaceCasingContext.Create());
        _grainFactory.ClearReceivedCalls();
        _client.ClearReceivedCalls();
        _gate.ClearReceivedCalls();
    }

    protected virtual IReactorMediator CreateMediator() => Substitute.For<IReactorMediator>();

#pragma warning disable CA2012 // Configuring the substitute; the ValueTask is the stubbed call, not an operation to await
    protected void ExhaustGate() => _gate.Enter(Arg.Any<CancellationToken>()).Returns(call => WaitForPermit(call.Arg<CancellationToken>()));
#pragma warning restore CA2012

    protected void Connect(string connectionId = "connection", CallContext? callContext = null, CancellationToken cancellationToken = default)
    {
        var messages = _messages = new Subject<ReactorMessage>();
        _connections.Add(messages);
        _connections.Add(_reactors.Observe(messages, callContext ?? new CallContext(new CallOptions(cancellationToken: cancellationToken))).Subscribe(_ => { }, error => _error.TrySetResult(error)));
        messages.OnNext(new(new(new RegisterReactor
        {
            ConnectionId = connectionId,
            EventStore = "store",
            Namespace = "namespace",
            Reactor = new ReactorDefinition { ReactorId = "reactor", EventSequenceId = "event-log" }
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
