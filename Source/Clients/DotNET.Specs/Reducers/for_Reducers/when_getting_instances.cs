// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_getting_instances : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; } = string.Empty;
    }

    class MyEvent;

    IReducerHandler _handler = null!;
    IReducerInvoker _invoker = null!;
    IEventSequence _eventSequence = null!;
    IEnumerable<object> _result = [];

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

        var eventSourceId1 = (EventSourceId)"source-1";
        var eventSourceId2 = (EventSourceId)"source-2";
        var events = new[]
        {
            new AppendedEvent(EventContext.EmptyWithEventSourceId(eventSourceId1.Value) with { SequenceNumber = 1 }, new MyEvent()),
            new AppendedEvent(EventContext.EmptyWithEventSourceId(eventSourceId2.Value) with { SequenceNumber = 2 }, new MyEvent())
        }.ToImmutableList();

        _eventSequence.GetFromSequenceNumber(EventSequenceNumber.First, null, _handler.EventTypes).Returns(events);

        _invoker.Invoke(Arg.Any<IServiceProvider>(), Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<object?>()).Returns(
            new ReduceResult(new MyReadModel { Name = "First" }, 1, [], string.Empty),
            new ReduceResult(new MyReadModel { Name = "Second" }, 2, [], string.Empty));
    }

    async Task Because() => _result = await _reducers.GetInstances(typeof(MyReadModel), EventCount.Unlimited);

    [Fact] void should_get_events_from_first_sequence_number() => _eventSequence.Received(1).GetFromSequenceNumber(EventSequenceNumber.First, null, _handler.EventTypes);
    [Fact] void should_invoke_reducer_for_each_event_source() => _invoker.Received(2).Invoke(Arg.Any<IServiceProvider>(), Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<object?>());
    [Fact] void should_return_reduced_instances() => _result.Count().ShouldEqual(2);
}
