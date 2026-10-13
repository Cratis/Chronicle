// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_passive_instances;

public class and_self_and_classic_filters_select_the_same_histories : given.a_passive_reducer
{
    readonly List<(int Amount, string Positions)[]> _pairs = [];
    readonly List<object?> _absent = [];

    async Task Because()
    {
        foreach (var (classic, self, expected) in new[]
        {
            (typeof(TagFiltered), typeof(SelfTags), 8),
            (typeof(Combined), typeof(SelfCombined), 3),
            (typeof(Typed), typeof(SelfTyped), 3),
            (typeof(Labelled), typeof(SelfLabelled), 308),
            (typeof(Unfiltered), typeof(SelfUnfiltered), 308),
            (typeof(DefaultSource), typeof(SelfDefault), 0)
        })
        {
            var pair = new List<(int, string)>();
            foreach (var type in new[] { classic, self })
            {
                var model = Configure(type);
                Seed(Event(1, 3, tags: ["blue"]), Event(2, 5, tags: ["green"]), Event(3, 100, tags: ["red"]), Event(4, 100, sourceType: "Other", tags: ["red"]), Event(5, 100, streamType: "Other", tags: ["red"]));
                var state = await _reducers.GetInstanceById(model, (ReadModelKey)"one");
                pair.Add(Summary(state));
                Summary(state).Amount.ShouldEqual(expected);
            }
            _pairs.Add(pair.ToArray());
        }
        foreach (var type in new[] { typeof(TagFiltered), typeof(SelfTags) })
        {
            var model = Configure(type);
            Seed(Event(1, 100, tags: ["red"]));
            _absent.Add(await _reducers.GetInstanceById(model, (ReadModelKey)"one"));
            Seed(Event(1, 100, tags: ["red"]), Event(2, 3, tags: ["blue"]), Event(3, 1000, tags: ["green"]));
            var bounded = (await _reducers.GetInstances(model, (EventCount)2)).Single();
            _pairs.Add([Summary(bounded), (3, "2,")]);
            Seed(Event(1, 3, tags: ["blue"]), Event(2, 7, source: "two", tags: ["green"]), Event(3, -1, tags: ["blue"]), Event(4, 2, tags: ["green"]));
            var states = (await _reducers.GetInstances(model)).Select(Summary).OrderBy(state => state.Amount).ToArray();
            _pairs.Add([states[0], (2, "4,")]);
            _pairs.Add([states[1], (7, "2,")]);
        }
    }

    Type Configure(Type type)
    {
        var self = ReducerCatalog.IsSelfReducer(type);
        var model = self ? type : typeof(Total);
        _artifactActivator.Activate(Arg.Any<IServiceProvider>(), type).Returns(new ActivatedArtifact(self ? new object() : Activator.CreateInstance(type)!, type, NullLogger<ActivatedArtifact>.Instance));
        var invoker = ReducerInvokerSelector.Create(new(type, model, self), new EventTypesForSpecifications([typeof(AmountAdded)]), _artifactActivator, "total");
        _handler = Substitute.For<IReducerHandler>();
        _handler.ReducerType.Returns(type);
        _handler.EventSequenceId.Returns((EventSequenceId)"passive-sequence");
        _handler.EventTypes.Returns(invoker.EventTypes);
        _handler.Invoker.Returns(invoker);
        _handlersByModelType[model] = _handler;
        return model;
    }

    static (int Amount, string Positions) Summary(object? state) => state is null ? (0, string.Empty) : ((int)state.GetType().GetProperty("Amount")!.GetValue(state)!, (string)state.GetType().GetProperty("Positions")!.GetValue(state)!);

    [Fact] void should_match_nonempty_golden_results_and_classic_metadata_selection() => _pairs.TrueForAll(pair => pair[0] == pair[1]).ShouldBeTrue();
    [Fact] void should_return_absent_for_both_all_rejected_histories() => _absent.TrueForAll(state => state is null).ShouldBeTrue();
    [Fact] void should_not_activate_any_self_model() => _artifactActivator.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "Activate" && call.GetArguments()[1] is Type type && ReducerCatalog.IsSelfReducer(type)).ShouldBeFalse();

    public record ObserverMetadata;
    [FilterEventsByTag("blue"), FilterEventsByTag("green")]
    public record TagMetadata : ObserverMetadata;
    [Passive]
    public record SelfTags(int Amount, string Positions) : TagMetadata
    {
        public static SelfTags? Reduce(AmountAdded @event, SelfTags? current, EventContext context) => @event.Amount == -1 ? null : new((current?.Amount ?? 0) + @event.Amount, $"{current?.Positions}{context.SequenceNumber.Value},");
    }
    [Passive, FilterEventsByTag("blue"), EventSourceType("Orders"), EventStreamType("Payments")]
    public record SelfCombined(int Amount, string Positions)
    {
        public static SelfCombined Reduce(AmountAdded @event, SelfCombined? current, EventContext context) => new((current?.Amount ?? 0) + @event.Amount, $"{current?.Positions}{context.SequenceNumber.Value},");
    }
    [Passive, FromEventSource<OrderSource>("Payments"), FilterEventsByTag("blue")]
    public record SelfTyped(int Amount, string Positions)
    {
        public static SelfTyped Reduce(AmountAdded @event, SelfTyped? current, EventContext context) => new((current?.Amount ?? 0) + @event.Amount, $"{current?.Positions}{context.SequenceNumber.Value},");
    }
    [Passive, Tag("blue"), Tags("green")]
    public record SelfLabelled(int Amount, string Positions)
    {
        public static SelfLabelled Reduce(AmountAdded @event, SelfLabelled? current, EventContext context) => new((current?.Amount ?? 0) + @event.Amount, $"{current?.Positions}{context.SequenceNumber.Value},");
    }
    [Passive]
    public record SelfUnfiltered(int Amount, string Positions)
    {
        public static SelfUnfiltered Reduce(AmountAdded @event, SelfUnfiltered? current, EventContext context) => new((current?.Amount ?? 0) + @event.Amount, $"{current?.Positions}{context.SequenceNumber.Value},");
    }
    [Passive, EventSourceType("Default")]
    public record SelfDefault(int Amount, string Positions)
    {
        public static SelfDefault Reduce(AmountAdded @event, SelfDefault? current, EventContext context) => new((current?.Amount ?? 0) + @event.Amount, $"{current?.Positions}{context.SequenceNumber.Value},");
    }
}
