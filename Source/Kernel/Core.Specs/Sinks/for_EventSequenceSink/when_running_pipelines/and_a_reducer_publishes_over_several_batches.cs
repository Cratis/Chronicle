// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_running_pipelines;

/// <summary>
/// Runs the real reducer pipeline against the event sequence sink, including redelivery of the first batch.
/// </summary>
public class and_a_reducer_publishes_over_several_batches : given.a_sink
{
    ReducerPipeline _pipeline;

    void Establish()
    {
        var compliance = Substitute.For<IReadModelsCompliance>();
        compliance.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo => Task.FromResult((ExpandoObject)callInfo[4]));
        compliance.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<ExpandoObject>())
            .Returns(callInfo => Task.FromResult((ExpandoObject)callInfo[3]));

        _pipeline = new ReducerPipeline(_definition, _sink, new ObjectComparer(), compliance, Store, Tenant);
    }

    async Task Because()
    {
        await _pipeline.Reduce(Context(5), Fold);
        await _pipeline.Reduce(Context(9), Fold);

        // Redelivery of the batch that was already folded.
        await _pipeline.Reduce(Context(9), Fold);
    }

    [Fact] void should_publish_one_instance_per_distinct_step() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).Count.ShouldEqual(2);
    [Fact] void should_not_publish_sink_bookkeeping() => _destinations.Events(Store, Tenant, EventSequenceId.Outbox).SelectMany(_ => ((IDictionary<string, object?>)_.Content).Keys).Where(_ => _.StartsWith("__", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_publish_the_accumulated_state() => ((IDictionary<string, object?>)_destinations.Events(Store, Tenant, EventSequenceId.Outbox)[^1].Content)["Count"]!.ToString().ShouldEqual("2");

    static Task<ReducerSubscriberResult> Fold(IEnumerable<AppendedEvent> events, ExpandoObject? initial)
    {
        var count = initial is not null && ((IDictionary<string, object?>)initial).TryGetValue("Count", out var existing) && existing is not null ? Convert.ToInt64(existing) : 0;
        var next = new ExpandoObject();
        ((IDictionary<string, object?>)next)["Total"] = (count + 1) * 10;
        ((IDictionary<string, object?>)next)["Count"] = count + 1;
        return Task.FromResult(new ReducerSubscriberResult(
            new ObserverSubscriberResult(ObserverSubscriberState.Ok, events.Last().Context.SequenceNumber, [], string.Empty),
            next));
    }

    static ReducerContext Context(ulong number) => new(
        [new AppendedEvent(
            EventContext.From("store", "tenant", new("Source", EventTypeGeneration.First), EventSourceType.Default, "key", EventStreamType.All, EventStreamId.Default, number, CorrelationId.New(), null, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            new ExpandoObject())],
        new Key("key", Properties.ArrayIndexers.NoIndexers));
}
