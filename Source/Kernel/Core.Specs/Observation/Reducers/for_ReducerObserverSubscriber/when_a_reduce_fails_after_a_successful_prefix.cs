// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using NSubstitute.ClearExtensions;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerObserverSubscriber;

/// <summary>
/// A reducer batch 10, 11, 12 fails at 12. The client reports 11 as its last successful observation but, being a
/// failure, sends no read-model state, so the pipeline writes nothing and the sink still holds the state from before
/// event 10. Reporting 11 as handled moves the observer past 10 and records the failure at 11, so recovery starts at
/// 11 from the unchanged sink state and the fold of event 10 is lost for good (Cratis/Chronicle#4540). Whatever the
/// subscriber reports as handled must be represented in the sink.
/// </summary>
public class when_a_reduce_fails_after_a_successful_prefix : given.a_subscriber_with_a_cached_pipeline
{
    RecordingSink _sink;
    AppendedEvent[] _events;
    ObserverSubscriberResult _result;

    void Establish()
    {
        _sink = new RecordingSink { Existing = null };
        var pipeline = new ReducerPipeline(
            CreateReadModelDefinition(),
            _sink,
            Substitute.For<IObjectComparer>(),
            Substitute.For<IReadModelsCompliance>(),
            _key.EventStore,
            _key.Namespace);

        _firstPipeline.Reduce(Arg.Any<ReducerContext>(), Arg.Any<ReducerDelegate>())
            .Returns(callInfo => pipeline.Reduce(callInfo.Arg<ReducerContext>(), callInfo.Arg<ReducerDelegate>()));

        _mediator.ClearSubstitute();
        _mediator.When(_ => _.OnNext(
                Arg.Any<ReducerId>(),
                Arg.Any<Cratis.Chronicle.Concepts.Clients.ConnectionId>(),
                Arg.Any<EventStoreName>(),
                Arg.Any<EventStoreNamespaceName>(),
                Arg.Any<ReduceOperation>(),
                Arg.Any<TaskCompletionSource<ReducerSubscriberResult>>()))
            .Do(callInfo => callInfo.Arg<TaskCompletionSource<ReducerSubscriberResult>>().SetResult(
                new ReducerSubscriberResult(ObserverSubscriberResult.Failed(11UL, "failed at 12"), null)));

        var eventType = new EventType("the-event", 1);
        _events =
        [
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(eventType, 10UL),
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(eventType, 11UL),
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(eventType, 12UL)
        ];
    }

    async Task Because() => _result = await _subscriber.OnNext("the-partition", _events, new ObserverSubscriberContext(new Cratis.Chronicle.Concepts.Clients.ConnectedClient()));

    [Fact] void should_have_failed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_report_events_as_handled_that_the_read_model_does_not_hold() => (_result.HandledAnyEvents && _sink.WriteModes.Count == 0).ShouldBeFalse();

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            "TestCollection",
            "Test Read Model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            "test-observer",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, new JsonSchema() } },
            []);
}
