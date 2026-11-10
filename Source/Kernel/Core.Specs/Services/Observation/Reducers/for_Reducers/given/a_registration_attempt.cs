// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Traces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.given;

public class a_registration_attempt : Observation.given.a_connection_held_on_another_silo
{
    protected ConnectionOwnership _ownership;
    protected IReducerMediator _mediator;
    protected IGrainFactory _grainFactory;
    protected IReadModel _definitionGrain;
    protected Exception _error;
    protected IConnectionReservation _reservation;
    readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _ownership = new();
        _mediator = Substitute.For<IReducerMediator>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _definitionGrain = Substitute.For<IReadModel>();
        _grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(_definitionGrain);
        _reservation = ReservationForCheck();
        _grainFactory.GetGrain<IConnectionReservation>(Arg.Any<string>(), Arg.Any<string>()).Returns(_reservation);
        var schema = new Schemas.JsonSchema();
        schema.Properties.Add("id", new Schemas.JsonSchemaProperty { Type = Schemas.JsonObjectType.String });
        _definitionGrain.GetDefinition().Returns(new Concepts.ReadModels.ReadModelDefinition(
            "read-model",
            "models",
            "Model",
            Concepts.ReadModels.ReadModelOwner.Client,
            Concepts.ReadModels.ReadModelSource.Code,
            Concepts.ReadModels.ReadModelObserverType.Reducer,
            Concepts.ReadModels.ReadModelObserverIdentifier.Unspecified,
            Concepts.Sinks.SinkDefinition.None,
            new Dictionary<Concepts.ReadModels.ReadModelGeneration, Schemas.JsonSchema> { [Concepts.ReadModels.ReadModelGeneration.First] = schema },
            []));
        var client = Substitute.For<IReducer>();
        _grainFactory.GetGrain<IReducer>(Arg.Any<string>()).Returns(client);
        client.SetDefinitionAndSubscribe(Arg.Any<Concepts.Observation.Reducers.ReducerDefinition>()).Returns(_ =>
        {
            _subscribed.TrySetResult();
            return Task.CompletedTask;
        });
        _grainFactory.ClearReceivedCalls();
        _definitionGrain.ClearReceivedCalls();
    }

    protected virtual IConnectionReservation ReservationForCheck() => Substitute.For<IConnectionReservation>();

    protected async Task Register(CallContext context, bool expectRegistration = false)
    {
        using var messages = new Subject<ReducerMessage>();
        var reducers = new Reducers(
            _grainFactory, _mediator, Substitute.For<IExpandoObjectConverter>(), new JsonSerializerOptions(), new ActivitySource<Reducers>(), Options.Create(new ChronicleOptions { Clustering = new() { ResponseTimeout = TimeSpan.FromMilliseconds(100) } }), _ownership, NullLogger<Reducers>.Instance, Substitute.For<IObserverRegistrationGate>(), NamespaceCasingContext.Create());
        using var cancellation = new CancellationTokenSource();
        var observed = reducers.Observe(messages, context).ToTask(cancellation.Token);
        messages.OnNext(new ReducerMessage(new Contracts.Primitives.OneOf<RegisterReducer, ReducerResult>(new RegisterReducer
        {
            ConnectionId = "connection",
            EventStore = "application",
            Namespace = "namespace",
            Reducer = new ReducerDefinition { ReducerId = "reducer", EventSequenceId = "event-log", ReadModel = "read-model" }
        })));
        if (expectRegistration)
        {
            _error = await Catch.Exception(() => _subscribed.Task.WaitAsync(TimeSpan.FromSeconds(5), _clock));
            await cancellation.CancelAsync();
            await Catch.Exception(() => observed);
        }
        else
        {
            _error = await Catch.Exception(() => observed.WaitAsync(TimeSpan.FromSeconds(5), _clock));
        }
    }
}
