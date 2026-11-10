// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using NSubstitute.ClearExtensions;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber.when_receiving_events;

/// <summary>
/// One partition's events beyond the bound the client declared are reduced page by page, each page from the state the page
/// before it wrote.
/// </summary>
public class and_the_client_declared_a_bound_smaller_than_the_events : given.a_subscriber_with_a_cached_pipeline
{
    static readonly EventStoreName _eventStore = "the-store";
    static readonly EventStoreNamespaceName _namespace = "the-namespace";
    readonly List<long> _initialCounts = [];
    InMemorySink _sink;
    ObserverSubscriberResult _result;

    void Establish()
    {
        var definition = new ReadModelDefinition(
            "counter",
            "Counters",
            "Counter",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            "counter-reducer",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.InMemory),
            new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, new JsonSchema() } },
            []);
        var compliance = Substitute.For<IReadModelsCompliance>();
        compliance.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo => Task.FromResult((ExpandoObject)callInfo[4]));
        compliance.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo => Task.FromResult((ExpandoObject)callInfo[3]));
        _sink = new InMemorySink(definition, new TypeFormats());
        var pipeline = new ReducerPipeline(definition, _sink, new ObjectComparer(), compliance, _eventStore, _namespace);
        _firstPipeline.Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>(), Arg.Any<bool>())
            .Returns(call => pipeline.Reduce(call.Arg<ReducerContext>(), call.Arg<ReducerDelegate>(), call.Arg<bool>()));

        _mediator.ClearSubstitute();
        _mediator.GetPageDelivery(Arg.Any<ReducerId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>())
            .Returns(new PageDeliveryDefinition(MaxEventsPerPage: 2));
        _mediator.When(mediator => mediator.OnNext(Arg.Any<ReducerId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReduceOperation>(), Arg.Any<TaskCompletionSource<ReducerSubscriberResult>>()))
            .Do(call =>
            {
                var operation = call.Arg<ReduceOperation>();
                var initial = CountOf(operation.InitialState);
                _initialCounts.Add(initial);
                var state = new ExpandoObject();
                ((IDictionary<string, object?>)state)["count"] = initial + operation.Events.Count();
                call.Arg<TaskCompletionSource<ReducerSubscriberResult>>().SetResult(new(ObserverSubscriberResult.Ok(operation.Events.Last().Context.SequenceNumber), state));
            });
    }

    async Task Because() => _result = await _subscriber.OnNext(
        "the-partition",
        [.. Enumerable.Range(1, 5).Select(sequenceNumber => CreateEvent((ulong)sequenceNumber))],
        new ObserverSubscriberContext(new ConnectedClient { ConnectionId = "A" }));

    [Fact] void should_reduce_three_pages() => _initialCounts.Count.ShouldEqual(3);
    [Fact] void should_reduce_the_first_page_from_no_state() => _initialCounts[0].ShouldEqual(0);
    [Fact] void should_reduce_the_second_page_from_what_the_first_wrote() => _initialCounts[1].ShouldEqual(2);
    [Fact] void should_reduce_the_third_page_from_what_the_second_wrote() => _initialCounts[2].ShouldEqual(4);
    [Fact] void should_fold_every_event_once() => CountOf(_sink.Collection["the-partition"]).ShouldEqual(5);
    [Fact] void should_report_the_last_event_as_handled() => _result.LastSuccessfulObservation.Value.ShouldEqual(5UL);

    static long CountOf(ExpandoObject? state) =>
        state is not null && ((IDictionary<string, object?>)state).TryGetValue("count", out var value) ? Convert.ToInt64(value) : 0L;

    static AppendedEvent CreateEvent(ulong sequenceNumber) =>
        new(
            EventContext.From(
                _eventStore,
                _namespace,
                EventType.Unknown,
                EventSourceType.Default,
                "the-partition",
                EventStreamType.All,
                EventStreamId.Default,
                sequenceNumber,
                CorrelationId.NotSet),
            new ExpandoObject());
}
