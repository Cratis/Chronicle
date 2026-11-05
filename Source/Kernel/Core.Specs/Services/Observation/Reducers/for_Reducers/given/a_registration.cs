// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Traces;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.given;

public class a_registration : Specification
{
    protected IGrainFactory _grainFactory;
    protected IReadModel _readModel;
    protected IReducer _client;
    protected IObserverRegistrationGate _gate;
    protected IDisposable _lease;
    protected readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _entering = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource<IDisposable> _permission = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource<Exception> _error = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<IDisposable> _connections = [];
    Reducers _reducers;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _client = Substitute.For<IReducer>();
        _grainFactory.GetGrain<IReducer>(Arg.Any<string>()).Returns(_client);
        _readModel = Substitute.For<IReadModel>();
        _grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(_readModel);
        var schema = new JsonSchema();
        schema.Properties.Add("id", new JsonSchemaProperty { Type = JsonObjectType.String });
        _readModel.GetDefinition().Returns(new ReadModelDefinition(
            "model",
            "models",
            "Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            ReadModelObserverIdentifier.Unspecified,
            Concepts.Sinks.SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []));
        _gate = Substitute.For<IObserverRegistrationGate>();
        _lease = Substitute.For<IDisposable>();
        _lease.When(_ => _.Dispose()).Do(_ => _released.TrySetResult());
#pragma warning disable CA2012 // Configuring the substitute; the ValueTask is the stubbed call, not an operation to await
        _gate.Enter(Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<IDisposable>(_lease));
#pragma warning restore CA2012
        _reducers = new(
            _grainFactory,
            Substitute.For<IReducerMediator>(),
            Substitute.For<IExpandoObjectConverter>(),
            new JsonSerializerOptions(),
            new ActivitySource<Reducers>(),
            Options.Create(new ChronicleOptions()),
            AllowingConnectionOwnership.Create(),
            NullLogger<Reducers>.Instance,
            _gate);
        _grainFactory.ClearReceivedCalls();
        _readModel.ClearReceivedCalls();
        _client.ClearReceivedCalls();
        _gate.ClearReceivedCalls();
    }

#pragma warning disable CA2012 // Configuring the substitute; the ValueTask is the stubbed call, not an operation to await
    protected void ExhaustGate() => _gate.Enter(Arg.Any<CancellationToken>()).Returns(call => WaitForPermit(call.Arg<CancellationToken>()));
#pragma warning restore CA2012

    protected void Connect(string connectionId = "connection", CancellationToken cancellationToken = default)
    {
        var messages = new Subject<ReducerMessage>();
        _connections.Add(messages);
        _connections.Add(_reducers.Observe(messages, new CallContext(new CallOptions(cancellationToken: cancellationToken))).Subscribe(_ => { }, error => _error.TrySetResult(error)));
        messages.OnNext(new(new(new RegisterReducer
        {
            ConnectionId = connectionId,
            EventStore = "store",
            Namespace = "namespace",
            Reducer = new ReducerDefinition { ReducerId = "reducer", EventSequenceId = "event-log", ReadModel = "model" }
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
