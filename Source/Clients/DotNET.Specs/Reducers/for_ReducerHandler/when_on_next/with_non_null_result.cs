// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_ReducerHandler.when_on_next;

public class with_non_null_result : given.a_reducer_handler
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    AppendedEvent _event;
    MyReadModel _reducedModel;
    ReduceResult _reduceResult;
    IServiceProvider _serviceProvider;

    void Establish()
    {
        _reducedModel = new MyReadModel { Name = "Reduced" };
        _reduceResult = new ReduceResult(_reducedModel, EventSequenceNumber.First, [], string.Empty);
        _serviceProvider = Substitute.For<IServiceProvider>();

        _invoker.ReadModelType.Returns(typeof(MyReadModel));
        _invoker.Invoke(_serviceProvider, Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<object>())
            .Returns(_reduceResult);

        _event = new AppendedEvent(
            new EventContext(
                (EventType)"test-event",
                EventSourceType.Default,
                (EventSourceId)"event-source-id",
                EventStreamType.All,
                (EventStreamId)EventStreamId.Default,
                EventSequenceNumber.First,
                DateTimeOffset.UtcNow,
                _eventStore.Name,
                _eventStore.Namespace,
                (CorrelationId)Guid.NewGuid(),
                [],
                Identities.Identity.NotSet),
            new { });
    }

    async Task Because() => await _handler.OnNext([_event], null, _serviceProvider);

    [Fact] void should_invoke_reducer() => _invoker.Received(1).Invoke(Arg.Any<IServiceProvider>(), Arg.Any<IEnumerable<EventAndContext>>(), Arg.Any<object>());
    [Fact]
    void should_notify_observers() => _reducerObservers.Received(1).NotifyChange(
        _eventStore.Namespace,
        Arg.Is<ReadModelKey>(k => k.Value == "event-source-id"),
        _reducedModel,
        false);
}
