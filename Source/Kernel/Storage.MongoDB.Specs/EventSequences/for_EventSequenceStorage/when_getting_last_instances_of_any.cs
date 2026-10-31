// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

[Collection(MongoDBCollection.Name)]
public class when_getting_last_instances_of_any(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    EventSequenceStorage _storage;
    IMongoCollection<Event> _intercepted;
    EventSourceId[] _sources;
    IReadOnlyDictionary<EventSourceId, Option<AppendedEvent>> _results;
    IReadOnlyDictionary<EventSourceId, Option<AppendedEvent>> _emptyTypes;
    bool _matchesSingles;

    async Task Establish()
    {
        await _database.EnsureIndexesForEventSequence(EventSequenceId.Log);
        var collection = _database.GetEventSequenceCollectionFor(EventSequenceId.Log);
        _sources = Enumerable.Range(0, 1001).Select(index => new EventSourceId($"source-{index}")).ToArray();
        await collection.InsertManyAsync(_sources.Select((source, index) => AnEvent((ulong)index, source, "parent")));
        await collection.InsertManyAsync([AnEvent(5000, _sources[0], "other-parent"), AnEvent(6000, _sources[0], "unrelated")]);
        _intercepted = Substitute.For<IMongoCollection<Event>>();
        _intercepted.DocumentSerializer.Returns(collection.DocumentSerializer);
        _intercepted.Settings.Returns(collection.Settings);
        _intercepted.FindAsync(Arg.Any<FilterDefinition<Event>>(), Arg.Any<FindOptions<Event, Event>>(), Arg.Any<CancellationToken>())
            .Returns(info => collection.FindAsync(info.ArgAt<FilterDefinition<Event>>(0), info.ArgAt<FindOptions<Event, Event>>(1)));
        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetEventSequenceCollectionFor(EventSequenceId.Log).Returns(_intercepted);
        var converter = Substitute.For<IEventConverter>();
        converter.ToAppendedEvent(Arg.Any<Event>()).Returns(info => Task.FromResult(AppendedEvent.EmptyWithEventSequenceNumber(info.Arg<Event>().SequenceNumber)));
        _storage = new EventSequenceStorage(_eventStore, _namespace, EventSequenceId.Log, database, converter, Substitute.For<IEventTypesStorage>(), Substitute.For<IIdentityStorage>(), Substitute.For<Json.IExpandoObjectConverter>(), JsonSerializerOptions.Default, Substitute.For<ILogger<EventSequenceStorage>>());
    }

    async Task Because()
    {
        _results = await _storage.GetLastInstancesOfAny(_sources.Concat([new("missing"), _sources[0]]), [new("parent"), new("other-parent")]);
        _matchesSingles = true;
        foreach (var (source, result) in _results)
        {
            var single = await _storage.TryGetLastInstanceOfAny(source, [new("parent"), new("other-parent")]);
            _matchesSingles &= result.TryGetValue(out var batched) == single.TryGetValue(out var individual) &&
                (batched is null || batched.Context.SequenceNumber == individual.Context.SequenceNumber);
        }
        _emptyTypes = await _storage.GetLastInstancesOfAny([_sources[0]], []);
    }

    [Fact] void should_equal_the_single_source_reads() => _matchesSingles.ShouldBeTrue();
    [Fact] void should_return_each_distinct_source_including_the_second_chunk() => _results.Count.ShouldEqual(1002);
    [Fact] void should_include_missing_sources() => _results[new EventSourceId("missing")].TryGetValue(out _).ShouldBeFalse();
    [Fact] void should_not_match_any_type_when_no_types_are_requested() => _emptyTypes[_sources[0]].TryGetValue(out _).ShouldBeFalse();
    [Fact] void should_issue_one_find_per_chunk() => _intercepted.Received(3).FindAsync(Arg.Any<FilterDefinition<Event>>(), Arg.Is<FindOptions<Event, Event>>(options => options.BatchSize == 1000 && options.Sort == null), Arg.Any<CancellationToken>());
    [Fact]
    void should_take_the_later_matching_event_in_the_whole_log()
    {
        _results[_sources[0]].TryGetValue(out var last).ShouldBeTrue();
        last.Context.SequenceNumber.ShouldEqual((EventSequenceNumber)5000UL);
    }

    static Event AnEvent(ulong number, EventSourceId source, EventTypeId type) => new(number, CorrelationId.New(), [], [], type, DateTimeOffset.UtcNow, EventSourceType.Default, source, EventStreamType.All, EventStreamId.Default, [], new Dictionary<string, BsonDocument>(), new Dictionary<string, string>(), []);
}
