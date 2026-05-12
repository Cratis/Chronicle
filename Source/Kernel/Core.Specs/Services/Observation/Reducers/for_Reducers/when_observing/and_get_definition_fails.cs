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
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers.when_observing;

public class and_get_definition_fails : Specification
{
    Subject<ReducerMessage> _messages;
    IReadModel _readModelGrain;
    Reducers _reducers;
    Exception _observedError;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        _readModelGrain = Substitute.For<IReadModel>();
        grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(_readModelGrain);
        _readModelGrain.GetDefinition()
            .Returns(Task.FromException<ReadModelDefinition>(new InvalidOperationException("Read model definition unavailable")));

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

    [Fact] void should_surface_an_error_on_the_observable() => _observedError.ShouldNotBeNull();
    [Fact] void should_wrap_it_as_a_reducer_registration_failure() => _observedError.ShouldBeOfExactType<ReducerRegistrationFailed>();
    [Fact] void should_include_registration_context_in_the_error_message() => _observedError.Message.ShouldContain("test-reducer");
}
