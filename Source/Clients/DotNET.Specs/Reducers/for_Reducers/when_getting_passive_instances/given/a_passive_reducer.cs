// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_passive_instances.given;

public class a_passive_reducer : for_Reducers.given.all_dependencies
{
    protected IEventSequence _sequence;
    protected IReducerHandler _handler;
    protected Total? _instance;
    protected IEnumerable<object> _instances;
    protected virtual Type ReducerType => typeof(TagFiltered);

    void Establish()
    {
        var sources = Substitute.For<IEventSources>();
        sources.GetFor(typeof(OrderSource)).Returns(new EventSourceDefinition(typeof(OrderSource), "Orders", string.Empty, ConcurrencyDimensions.None, [new("Payments", string.Empty, ConcurrencyDimensions.None)]));
        _eventStore.EventSources.Returns(sources);
        _handler = Substitute.For<IReducerHandler>();
        _handler.ReducerType.Returns(ReducerType);
        _handler.EventSequenceId.Returns((EventSequenceId)"passive-sequence");
        _artifactActivator.Activate(Arg.Any<IServiceProvider>(), ReducerType).Returns(call => new ActivatedArtifact(Activator.CreateInstance(ReducerType)!, ReducerType, NullLogger<ActivatedArtifact>.Instance));
        var invoker = new ReducerInvoker(new EventTypesForSpecifications([typeof(AmountAdded)]), _artifactActivator, ReducerType, typeof(Total), "total");
        _handler.Invoker.Returns(invoker);
        _handler.EventTypes.Returns(invoker.EventTypes);
        _handlersByModelType[typeof(Total)] = _handler;
        _sequence = Substitute.For<IEventSequence>();
        _eventStore.GetEventSequence(_handler.EventSequenceId).Returns(_sequence);
    }

    protected void Seed(params AppendedEvent[] events)
    {
        _sequence.GetForEventSourceIdAndEventTypes((EventSourceId)"one", _handler.EventTypes).Returns(events.Where(input => input.Context.EventSourceId.Value == "one").ToImmutableList());
        _sequence.GetFromSequenceNumber(EventSequenceNumber.First, null, _handler.EventTypes).Returns(events.ToImmutableList());
    }

    protected static AppendedEvent Event(ulong position, int amount, string source = "one", string sourceType = "Orders", string streamType = "Payments", params string[] tags) =>
        new(
            EventContext.EmptyWithEventSourceId(source) with
            {
                SequenceNumber = position,
                EventSourceType = sourceType,
                EventStreamType = streamType,
                Tags = tags.Select(tag => new Tag(tag)).ToArray()
            },
            new AmountAdded(amount));

    protected async Task ReadById() => _instance = (Total?)await _reducers.GetInstanceById(typeof(Total), (ReadModelKey)"one");

    protected async Task ReadAll(EventCount? count = null) => _instances = await _reducers.GetInstances(typeof(Total), count);

    [EventType]
    public record AmountAdded(int Amount);

    [Passive]
    public record Total(int Amount, string Positions);

    public abstract class Fold : IReducerFor<Total>
    {
        public Total? Apply(AmountAdded @event, Total? current, EventContext context) => @event.Amount == -1 ? null : new((current?.Amount ?? 0) + @event.Amount, $"{current?.Positions}{context.SequenceNumber.Value},");
    }

    [FilterEventsByTag("blue"), FilterEventsByTag("green")]
    public class TagFiltered : Fold;

    [FilterEventsByTag("blue"), EventSourceType("Orders"), EventStreamType("Payments")]
    public class Combined : Fold;

    [FromEventSource<OrderSource>("Payments"), FilterEventsByTag("blue")]
    public class Typed : Fold;

    [Tag("blue"), Tags("green")]
    public class Labelled : Fold;

    [EventSourceType("Default")]
    public class DefaultSource : Fold;

    public class Unfiltered : Fold;

    public class OrderSource : IEventSource;
}
