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

/// <summary>
/// Per-type predecessor seeks must merge into one latest predecessor for each source.
/// </summary>
/// <param name="fixture">The shared MongoDB fixture.</param>
[Collection(MongoDBCollection.Name)]
public class when_reading_parent_history_across_multiple_types(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    EventSequenceStorage _storage;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _window;
    IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>> _predecessors;

    async Task Establish()
    {
        var types = Substitute.For<IEventTypesStorage>();
        var identities = Substitute.For<IIdentityStorage>();
        var expandoConverter = Substitute.For<Json.IExpandoObjectConverter>();
        var converter = new EventConverter(_eventStore, _namespace, types, identities, expandoConverter);
        _storage = new EventSequenceStorage(_eventStore, _namespace, EventSequenceId.Log, _database, converter, types, identities, expandoConverter, JsonSerializerOptions.Default, NullLogger<EventSequenceStorage>.Instance);
        await _database.GetEventSequenceCollectionFor(EventSequenceId.Log).InsertManyAsync(
        [
            AnEvent(0, "source", "first-parent"),
            AnEvent(1, "source", "second-parent"),
            AnEvent(2, "other-source", "second-parent"),
            AnEvent(3, "other-source", "first-parent"),
            AnEvent(4, "source", "first-parent"),
            AnEvent(5, "source", "second-parent"),
            AnEvent(6, "source", "excluded")
        ]);
    }

    async Task Because()
    {
        _window = await _storage.GetInstancesOfAnyInRange(["source", "other-source", "missing"], ["first-parent", "second-parent"], 4, 6);
        _predecessors = await _storage.GetInstancesOfAnyInRange(["source", "other-source"], ["first-parent", "second-parent"], 4, 3);
    }

    [Fact] void should_choose_the_latest_predecessor_across_types_before_the_window() => _window["source"].Select(entry => entry.Context.SequenceNumber.Value).ShouldEqual([1UL, 4UL, 5UL]);
    [Fact] void should_choose_a_different_type_for_another_sources_latest_predecessor() => _window["other-source"].Single().Context.SequenceNumber.Value.ShouldEqual(3UL);
    [Fact] void should_return_only_one_predecessor_per_source_when_the_window_is_empty() => _predecessors.Values.Select(entries => entries.Single().Context.SequenceNumber.Value).Order().ShouldEqual([1UL, 3UL]);
    [Fact] void should_omit_missing_sources() => _window.Count.ShouldEqual(2);

    static Event AnEvent(ulong number, string source, string type) => new(
        number,
        CorrelationId.New(),
        [],
        [],
        type,
        DateTimeOffset.UnixEpoch,
        EventSourceType.Default,
        source,
        EventStreamType.All,
        EventStreamId.Default,
        [],
        new Dictionary<string, BsonDocument> { ["1"] = new() },
        new Dictionary<string, string>(),
        []);
}
