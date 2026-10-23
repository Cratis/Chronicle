// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

[Collection(MongoDBCollection.Name)]
public class when_reading_a_bounded_parent_history(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    EventSequenceStorage _storage;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _result;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _predecessor;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _singleSourceWindow;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _empty;

    async Task Establish()
    {
        var types = Substitute.For<IEventTypesStorage>();
        var identities = Substitute.For<IIdentityStorage>();
        var expandoConverter = Substitute.For<Json.IExpandoObjectConverter>();
        var converter = new EventConverter(_eventStore, _namespace, types, identities, expandoConverter);
        _storage = new EventSequenceStorage(_eventStore, _namespace, EventSequenceId.Log, _database, converter, types, identities, expandoConverter, JsonSerializerOptions.Default, NullLogger<EventSequenceStorage>.Instance);
        await _database.GetEventSequenceCollectionFor(EventSequenceId.Log).InsertManyAsync(
            Enumerable.Range(0, 2000).Select(i => AnEvent((ulong)i)).Concat(
            [
                AnEvent(2000) with { Type = "excluded" },
                AnEvent(2001) with { EventSourceId = "other-source" }
            ]));
    }

    async Task Because()
    {
        _result = await _storage.GetInstancesOfAnyInRange(["source", "source", "other-source", "missing"], ["type"], 1997, 2001);
        _predecessor = await _storage.GetInstancesOfAnyInRange(["source"], ["type"], 1997, 1996);
        _singleSourceWindow = await _storage.GetInstancesOfAnyInRange(["source"], ["type"], 1997, 2001);
        _empty = await _storage.GetInstancesOfAnyInRange(["source"], [], 0, 2001);
    }

    [Fact] void should_return_only_the_latest_predecessor_and_the_inclusive_window() => _result["source"].Select(e => e.Context.SequenceNumber.Value).ShouldEqual([1996UL, 1997UL, 1998UL, 1999UL]);
    [Fact] void should_match_across_generations() => _result["source"].Select(e => e.Context.EventType.Generation.Value).ShouldEqual([1U, 2U, 1U, 2U]);
    [Fact] void should_include_the_upper_boundary_for_the_other_source() => _result["other-source"].Single().Context.SequenceNumber.Value.ShouldEqual(2001UL);
    [Fact] void should_omit_missing_sources_and_deduplicate_requested_sources() => _result.Count.ShouldEqual(2);
    [Fact] void should_support_a_single_predecessor_lookup_without_loading_the_history() => _predecessor["source"].Single().Context.SequenceNumber.Value.ShouldEqual(1996UL);
    [Fact] void should_combine_the_single_source_predecessor_and_window() => _singleSourceWindow["source"].Select(e => e.Context.SequenceNumber.Value).ShouldEqual([1996UL, 1997UL, 1998UL, 1999UL]);
    [Fact] void should_return_no_matches_for_no_types() => _empty.Count.ShouldEqual(0);

    static Event AnEvent(ulong number) => new(
        number,
        CorrelationId.New(),
        [],
        [],
        "type",
        DateTimeOffset.UnixEpoch,
        EventSourceType.Default,
        "source",
        EventStreamType.All,
        EventStreamId.Default,
        [],
        new Dictionary<string, BsonDocument> { [number % 2 == 0 ? "1" : "2"] = new() },
        new Dictionary<string, string>(),
        []);
}
