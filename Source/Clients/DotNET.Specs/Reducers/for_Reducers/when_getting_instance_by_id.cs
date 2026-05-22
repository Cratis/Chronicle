// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_getting_instance_by_id : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; } = string.Empty;
    }

    class MyEvent;

    readonly ReadModelKey _readModelKey = "event-source-id";
    IReducerHandler _handler = null!;
    IReducerInvoker _invoker = null!;
    IEventSequence _eventSequence = null!;
    object? _result;

    void Establish()
    {
        _handler = Substitute.For<IReducerHandler>();
        _handler.EventSequenceId.Returns(new EventSequenceId("my-sequence"));
        _handler.EventTypes.Returns([new EventType("my-event-type", 1)]);

        _invoker = Substitute.For<IReducerInvoker>();
        _handler.Invoker.Returns(_invoker);
        _handlersByModelType[typeof(MyReadModel)] = _handler;

        _eventSequence = Substitute.For<IEventSequence>();
        _eventStore.GetEventSequence(_handler.EventSequenceId).Returns(_eventSequence);

        var events = new[]
        {
            new AppendedEvent(
                EventContext.EmptyWithEventSourceId(_readModelKey.Value) with { SequenceNumber = 42 },
                new MyEvent())
        }.ToImmutableList();

        _eventSequence.GetForEventSourceIdAndEventTypes((EventSourceId)_readModelKey.Value, _handler.EventTypes)
            .Returns(events);

        _invoker.Invoke(Arg.Any<IServiceProvider>(), Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<object?>())
            .Returns(new ReduceResult(new MyReadModel { Name = "Reduced" }, 42, [], string.Empty));
    }

    async Task Because() => _result = await _reducers.GetInstanceById(typeof(MyReadModel), _readModelKey);

    [Fact] void should_get_events_for_read_model_key() => _eventSequence.Received(1).GetForEventSourceIdAndEventTypes((EventSourceId)_readModelKey.Value, _handler.EventTypes);
    [Fact] void should_invoke_reducer() => _invoker.Received(1).Invoke(Arg.Any<IServiceProvider>(), Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<object?>());
    [Fact] void should_return_reduced_read_model() => (_result as MyReadModel)?.Name.ShouldEqual("Reduced");
}
