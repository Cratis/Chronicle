// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelFolder.when_folding_original_contexts;

public class and_stored_tags_override_current_type_annotations : Specification
{
    EventStoreForTesting _store;
    IReadOnlyDictionary<EventSourceId, SelfState> _self;
    IReadOnlyDictionary<EventSourceId, ClassicState> _classic;
    ulong _syntheticEventCount;

    void Establish() => _store = new(null, new InMemoryClientArtifactsProvider
    {
        EventTypes = [typeof(Added)],
        Reducers = [typeof(SelfState), typeof(Classic)]
    });

    async Task Because()
    {
        var type = _store.EventTypes.GetEventTypeFor(typeof(Added));
        var events = new[]
        {
            new AppendedEvent(
                EventContext.EmptyWithEventSourceId("excluded") with
                {
                    EventType = type,
                    SequenceNumber = 0,
                    Tags = [new Tag("rejected")]
                },
                new Added(100)),
            new AppendedEvent(
                EventContext.EmptyWithEventSourceId("accepted") with
                {
                    EventType = type,
                    SequenceNumber = 1,
                    Tags = [new Tag("accepted")]
                },
                new Added(3))
        };
        _self = await ReadModelFolder.Fold<SelfState>(_store, events, useOriginalContexts: true);
        _classic = await ReadModelFolder.Fold<ClassicState>(_store, events, useOriginalContexts: true);
        var synthetic = _store.GetEventSequence("testing-read-model-input");
        _syntheticEventCount = (ulong)(await synthetic.GetFromSequenceNumber(EventSequenceNumber.First)).Count;
    }

    void Destroy() => _store.Dispose();

    [Fact] void should_keep_the_accepted_self_source_key() => _self[(EventSourceId)"accepted"].Total.ShouldEqual(3);
    [Fact] void should_keep_the_accepted_classic_source_key() => _classic[(EventSourceId)"accepted"].Total.ShouldEqual(3);
    [Fact] void should_not_invent_accepted_tags_from_the_self_events_current_clr_type() => _self.ContainsKey("excluded").ShouldBeFalse();
    [Fact] void should_not_invent_accepted_tags_from_the_classic_events_current_clr_type() => _classic.ContainsKey("excluded").ShouldBeFalse();
    [Fact] void should_not_persist_events_read_from_the_store_log_as_synthetic_input() => _syntheticEventCount.ShouldEqual(0UL);

    [EventType, Tag("accepted")]
    public record Added(int Amount);
    [FilterEventsByTag("accepted")]
    public record SelfState(int Total)
    {
        public static SelfState Reduce(Added e, SelfState? current) => new((current?.Total ?? 0) + e.Amount);
    }
    public record ClassicState(int Total);
    [FilterEventsByTag("accepted")]
    public class Classic : IReducerFor<ClassicState>
    {
        public ClassicState Apply(Added e, ClassicState? current) => new((current?.Total ?? 0) + e.Amount);
    }
}
