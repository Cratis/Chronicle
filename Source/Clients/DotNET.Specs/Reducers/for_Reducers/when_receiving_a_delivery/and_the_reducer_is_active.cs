// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Receivers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_receiving_a_delivery;

public class and_the_reducer_is_active : given.all_dependencies
{
    ReducerResult _result;
    AppendedEvent _event;
    PersonRegistered _initialState;
    System.Diagnostics.ActivitySource _traceSource;
    protected override Observation.EventGenerationDelivery GenerationDelivery => Observation.EventGenerationDelivery.Pinned;

    void Establish()
    {
        _traceSource = new("receiver-reducer-specification");
        _activitySource = new Cratis.Traces.ActivitySource<Reducers>(_traceSource);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(_serviceProvider);
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        _jsonSerializerOptions.PropertyNameCaseInsensitive = true;
        _eventTypes.GetClrTypeFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(typeof(PersonRegistered));
        var handler = Substitute.For<IReducerHandler>();
        handler.ReadModelType.Returns(typeof(PersonRegistered));
        handler.Id.Returns((ReducerId)"pinned");
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        handler.IsActive.Returns(true);
        handler.EventTypes.Returns([new EventType("person-registered", 2)]);
        handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>()).Returns(call =>
        {
            _event = call.Arg<IEnumerable<AppendedEvent>>().Single();
            _initialState = (PersonRegistered)call.ArgAt<object>(1);
            return new ReduceResult(new PersonRegistered("after"), 12, [], string.Empty);
        });
        _handlersByModelType[typeof(PersonRegistered)] = handler;
        _reducers = CreateReducers();
    }

    async Task Because() => _result = await ((IReceiveReducerDeliveries)_reducers).Deliver("pinned", new ReduceOperationMessage
    {
        Partition = "person",
        InitialState = "{\"fullName\":\"before\"}",
        Events = [new Contracts.Events.AppendedEvent
        {
            Context = (EventContext.Empty with { EventType = new("person-registered", 2), SequenceNumber = 12, AppendedGeneration = 1 }).ToContract(),
            Content = "{\"fullName\":\"released\"}",
            GenerationalContent = new Dictionary<int, string> { [2] = "{\"fullName\":\"protected\"}" }
        }]
    });

    [Fact] void should_deserialize_the_initial_state() => _initialState.FullName.ShouldEqual("before");
    [Fact] void should_use_the_released_pinned_content() => ((PersonRegistered)_event.Content).FullName.ShouldEqual("released");
    [Fact] void should_keep_the_appended_generation() => _event.Context.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_report_success() => _result.State.ShouldEqual(ObservationState.Success);
    [Fact] void should_return_the_read_model_state() => _result.ReadModelState.ShouldEqual("{\"FullName\":\"after\"}");
    [Fact] void should_keep_the_last_observation() => _result.LastSuccessfulObservation.ShouldEqual(12UL);

    void Destroy() => _traceSource?.Dispose();

    public record PersonRegistered(string FullName);
}
