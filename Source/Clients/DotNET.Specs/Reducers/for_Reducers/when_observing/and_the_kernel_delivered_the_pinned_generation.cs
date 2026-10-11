// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_observing;

public class and_the_kernel_delivered_the_pinned_generation : given.all_dependencies
{
    Subject<ReduceOperationMessage> _observations;
    readonly TaskCompletionSource<AppendedEvent> _received = new(TaskCreationOptions.RunContinuationsAsynchronously);
    AppendedEvent _event;
    System.Diagnostics.ActivitySource _traceSource;
    protected override Observation.EventGenerationDelivery GenerationDelivery => Observation.EventGenerationDelivery.Pinned;

    async Task Establish()
    {
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(Connections.ConnectionId.New());
        _traceSource = new System.Diagnostics.ActivitySource("pinned-reducer-specification");
        _activitySource = new Cratis.Traces.ActivitySource<Reducers>(_traceSource);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(_serviceProvider);
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        _jsonSerializerOptions.PropertyNameCaseInsensitive = true;
        _eventTypes.GetClrTypeFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(typeof(PersonRegistered));
        var handler = Substitute.For<IReducerHandler>();
        handler.ReducerType.Returns(typeof(object));
        handler.ReadModelType.Returns(typeof(PersonRegistered));
        handler.Id.Returns((ReducerId)"pinned");
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        handler.IsActive.Returns(true);
        handler.EventTypes.Returns([new EventType("person-registered", 2)]);
        handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>()).Returns(call =>
        {
            _received.TrySetResult(call.Arg<IEnumerable<AppendedEvent>>().Single());
            return new ReduceResult(null, EventSequenceNumber.Unavailable, [], string.Empty);
        });
        _handlersByModelType[typeof(PersonRegistered)] = handler;
        _observations = new();
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(_observations);
        _reducers = CreateReducers();
        await _reducers.Register();
    }

    async Task Because()
    {
        _observations.OnNext(new ReduceOperationMessage
        {
            Partition = "person",
            Events = [new Contracts.Events.AppendedEvent
            {
                Context = (EventContext.Empty with { EventType = new("person-registered", 2), AppendedGeneration = 1 }).ToContract(),
                Content = "{\"fullName\":\"Ada Lovelace\"}",
                GenerationalContent = new Dictionary<int, string> { [2] = "{\"fullName\":\"ciphertext\"}" }
            }]
        });
        _event = await _received.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_use_the_released_content_not_the_protected_generation_map() => ((PersonRegistered)_event.Content).FullName.ShouldEqual("Ada Lovelace");
    [Fact] void should_keep_the_appended_generation() => _event.Context.AppendedGeneration!.Value.ShouldEqual(1U);

    void Destroy()
    {
        _observations.OnCompleted();
        _observations.Dispose();
        _traceSource.Dispose();
    }

    public record PersonRegistered(string FullName);
}
