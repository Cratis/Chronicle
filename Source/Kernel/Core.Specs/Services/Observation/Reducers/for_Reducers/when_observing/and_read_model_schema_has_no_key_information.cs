// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.when_observing;

public class and_read_model_schema_has_no_key_information : Specification
{
    Subject<ReducerMessage> _messages;
    Reducers _reducers;
    Exception _observedError;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var readModel = Substitute.For<IReadModel>();
        grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(readModel);
        readModel.GetDefinition()
            .Returns(new ReadModelDefinition(
                "test-read-model",
                "test-container",
                "test-display-name",
                ReadModelOwner.Client,
                ReadModelSource.Code,
                ReadModelObserverType.Reducer,
                ReadModelObserverIdentifier.Unspecified,
                Concepts.Sinks.SinkDefinition.None,
                new Dictionary<ReadModelGeneration, JsonSchema>
                {
                    { ReadModelGeneration.First, new JsonSchema() }
                },
                []));

        _reducers = new Reducers(
            grainFactory,
            Substitute.For<IReducerMediator>(),
            Substitute.For<IExpandoObjectConverter>(),
            new JsonSerializerOptions(),
            NullLogger<Reducers>.Instance);

        _messages = new Subject<ReducerMessage>();
    }

    async Task Because()
    {
        var errorSeen = new TaskCompletionSource();
        var observable = _reducers.Observe(_messages, default);
        observable.Subscribe(
            _ => { },
            ex =>
            {
                _observedError = ex;
                errorSeen.TrySetResult();
            },
            () => { });

        _messages.OnNext(new ReducerMessage(new OneOf<RegisterReducer, ReducerResult>(new RegisterReducer
        {
            ConnectionId = "test-connection",
            EventStore = "test-store",
            Namespace = "test-namespace",
            Reducer = new ReducerDefinition
            {
                ReducerId = "test-reducer",
                EventSequenceId = "event-log",
                ReadModel = "test-read-model"
            }
        })));

        await errorSeen.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_fail_registration_before_subscribing() => _observedError.ShouldBeOfExactType<ReducerRegistrationFailed>();
    [Fact] void should_describe_the_key_schema_problem() => _observedError.Message.ShouldContain("No key property could be inferred");
}
