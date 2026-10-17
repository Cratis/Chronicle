// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Keys;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Keys.for_ObserverKeyIndexes.when_getting_keys;

/// <summary>
/// The scope is the caller's, per namespace: the same observer in two namespaces reads what each namespace's scope says,
/// not what a definition looked up once would say for both.
/// </summary>
public class and_two_namespaces_have_different_scopes : Specification
{
    static readonly EventType _eventType = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    FilterDefinition<Event> _wildcardFilter;
    FilterDefinition<Event> _explicitFilter;

    async Task Because()
    {
        var observerKey = new ObserverKey("observer", "store", "namespace", EventSequenceId.Log);
        var first = new ObserverKeyIndexes(DatabaseRecordingInto(filter => _wildcardFilter = filter));
        var second = new ObserverKeyIndexes(DatabaseRecordingInto(filter => _explicitFilter = filter));

        await Enumerate(await first.GetFor(observerKey, ObserverReaderScope.Wildcard));
        await Enumerate(await second.GetFor(observerKey, ObserverReaderScope.Explicit([_eventType])));
    }

    [Fact] void should_not_narrow_the_namespace_with_a_wildcard() => Render(_wildcardFilter).ShouldNotContain("$in");
    [Fact] void should_narrow_the_namespace_with_an_explicit_scope() => Render(_explicitFilter).ShouldContain("$in");

    static string Render(FilterDefinition<Event> filter) =>
        filter.Render(new RenderArgs<Event>(BsonSerializer.LookupSerializer<Event>(), BsonSerializer.SerializerRegistry)).AsBsonDocument.ToString();

    static async Task Enumerate(IObserverKeyIndex index) => await index.GetKeys(EventSequenceNumber.First).GetAsyncEnumerator().MoveNextAsync();

    static IEventStoreNamespaceDatabase DatabaseRecordingInto(Action<FilterDefinition<Event>> record)
    {
        var cursor = Substitute.For<IAsyncCursor<EventSourceId>>();
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(false);

        var collection = Substitute.For<IMongoCollection<Event>>();
        collection
            .DistinctAsync(
                Arg.Any<FieldDefinition<Event, EventSourceId>>(),
                Arg.Any<FilterDefinition<Event>>(),
                Arg.Any<DistinctOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                record(call.Arg<FilterDefinition<Event>>());
                return Task.FromResult(cursor);
            });

        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetEventSequenceCollectionFor(Arg.Any<EventSequenceId>()).Returns(collection);
        return database;
    }
}
