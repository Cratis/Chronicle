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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.given;

public class a_registration_stream : Specification
{
    protected RegisterReducer _registration;
    protected Concepts.Observation.Reducers.ReducerDefinition _definition;
    protected Exception _error;
    protected IReducer _reducer;
    protected IReadModel _readModel;
    readonly Subject<ReducerMessage> _messages = new();
    readonly TaskCompletionSource _registered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IDisposable _subscription;

    void Establish()
    {
        _registration = new RegisterReducer
        {
            ConnectionId = "connection",
            EventStore = "store",
            Namespace = "namespace",
            Reducer = new Contracts.Observation.Reducers.ReducerDefinition { ReducerId = "reducer", EventSequenceId = "event-log", ReadModel = "model" }
        };
        _reducer = Substitute.For<IReducer>();
        _reducer.SetDefinitionAndSubscribe(Arg.Any<Concepts.Observation.Reducers.ReducerDefinition>()).Returns(call =>
        {
            _definition = call.Arg<Concepts.Observation.Reducers.ReducerDefinition>();
            _registered.TrySetResult();
            return Task.CompletedTask;
        });
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IReducer>(Arg.Any<string>()).Returns(_reducer);
        _readModel = Substitute.For<IReadModel>();
        grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(_readModel);
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
        var reducers = new Reducers(
            grainFactory,
            Substitute.For<IReducerMediator>(),
            Substitute.For<IExpandoObjectConverter>(),
            new JsonSerializerOptions(),
            new ActivitySource<Reducers>(),
            Options.Create(new ChronicleOptions()),
            NullLogger<Reducers>.Instance);
        _subscription = reducers.Observe(_messages).Subscribe(_ => { }, error =>
        {
            _error = error;
            _registered.TrySetResult();
        });
    }

    protected async Task Register()
    {
        _messages.OnNext(new(new(_registration)));
        await _registered.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy()
    {
        _subscription.Dispose();
        _messages.Dispose();
    }
}
