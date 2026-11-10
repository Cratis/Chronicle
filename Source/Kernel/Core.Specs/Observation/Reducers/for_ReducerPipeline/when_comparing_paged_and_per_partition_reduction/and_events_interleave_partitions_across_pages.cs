// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.InMemory.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_comparing_paged_and_per_partition_reduction;

/// <summary>
/// The same events, reduced one partition at a time into one sink and in pages spanning partitions into another, leave both
/// sinks holding identical read models - including a partition the client fails on its first attempt and succeeds on its next.
/// </summary>
public class and_events_interleave_partitions_across_pages : Specification
{
    static readonly EventStoreName _eventStore = "store";
    static readonly EventStoreNamespaceName _namespace = "namespace";
    static readonly string[] _partitions = ["alpha", "beta", "gamma", "delta"];

    InMemorySink _perPartitionSink;
    InMemorySink _pagedSink;
    ReducerPipeline _perPartition;
    ReducerPipeline _paged;
    AppendedEvent[] _events;
    int _pagedAttemptsOnBeta;
    int _perPartitionAttemptsOnBeta;

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

        _perPartitionSink = new InMemorySink(definition, new TypeFormats());
        _pagedSink = new InMemorySink(definition, new TypeFormats());
        _perPartition = new ReducerPipeline(definition, _perPartitionSink, new ObjectComparer(), compliance, _eventStore, _namespace);
        _paged = new ReducerPipeline(definition, _pagedSink, new ObjectComparer(), compliance, _eventStore, _namespace);

        // Twelve events spread over four partitions, interleaved, so each page holds several partitions and each partition several pages.
        _events = [.. Enumerable.Range(0, 12).Select(index => CreateEvent(PartitionAt(index), (ulong)index))];
    }

    async Task Because()
    {
        var redelivered = false;
        foreach (var page in _events.Chunk(5))
        {
            var contexts = Batches(page).ToArray();
            await ReducePerPartition(contexts);
            await ReducePaged(contexts);

            // The partition the client failed is delivered again from its stored state before anything later of it, as the observer would.
            if (!redelivered && contexts.FirstOrDefault(_ => Equals(_.Key.Value, "beta")) is { } failed)
            {
                redelivered = true;
                await ReducePerPartition([failed]);
                await ReducePaged([failed]);
            }
        }
    }

    [Fact] void should_hold_the_same_partitions() => _pagedSink.Collection.Keys.ShouldContainOnly(_perPartitionSink.Collection.Keys);
    [Fact] void should_hold_the_same_state_for_every_partition() => _partitions.All(partition => Count(_pagedSink, partition) == Count(_perPartitionSink, partition)).ShouldBeTrue();
    [Fact] void should_have_folded_every_event_once() => _partitions.Sum(partition => Count(_pagedSink, partition)).ShouldEqual(_events.Length);

    static string PartitionAt(int index)
    {
        var spread = index * 3;
        return _partitions[spread % _partitions.Length];
    }

    static long Count(InMemorySink sink, string partition) =>
        Convert.ToInt64(((IDictionary<string, object?>)sink.Collection[partition])["count"]);

    static IEnumerable<ReducerContext> Batches(IEnumerable<AppendedEvent> page) =>
        page.GroupBy(_ => _.Context.EventSourceId.Value)
            .Select(group => new ReducerContext([.. group.OrderBy(_ => _.Context.SequenceNumber.Value)], (Key)group.Key));

    static ReducerSubscriberResult Fold(IEnumerable<AppendedEvent> events, ExpandoObject? initial, bool fail)
    {
        if (fail)
        {
            return new(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "boom"), null);
        }

        var count = initial is not null && ((IDictionary<string, object?>)initial).TryGetValue("count", out var value) ? Convert.ToInt64(value) : 0L;
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = count + events.Count();
        return new(ObserverSubscriberResult.Ok(events.Last().Context.SequenceNumber), state);
    }

    static AppendedEvent CreateEvent(string eventSourceId, ulong sequenceNumber) =>
        new(
            EventContext.From(
                _eventStore,
                _namespace,
                EventType.Unknown,
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                sequenceNumber,
                CorrelationId.NotSet),
            new ExpandoObject());

    async Task ReducePerPartition(IEnumerable<ReducerContext> contexts)
    {
        foreach (var context in contexts)
        {
            var fail = Equals(context.Key.Value, "beta") && _perPartitionAttemptsOnBeta++ == 0;
            await _perPartition.Reduce(context, (events, initial) => Task.FromResult(Fold(events, initial, fail)));
        }
    }

    Task ReducePaged(IReadOnlyList<ReducerContext> contexts) =>
        _paged.ReducePage(contexts, partitions =>
        {
            IReadOnlyDictionary<Key, ReducerSubscriberResult> results = partitions.ToDictionary(
                _ => _.Key,
                _ => Fold(_.Events, _.InitialState, Equals(_.Key.Value, "beta") && _pagedAttemptsOnBeta++ == 0));
            return Task.FromResult(results);
        });
}
