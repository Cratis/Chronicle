// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_observing_instances;

/// <summary>
/// A read that runs while a replay promotion has the primary collection renamed aside - the primary collection is absent
/// and the promoting collection exists - is not an empty read model, so nothing is read or emitted; the read that
/// follows the rename into place emits the page.
/// </summary>
public class and_a_promotion_is_in_progress : given.a_sink_observing_a_container
{
    int _pagesWhilePromoting;

    void Establish() => _promotingExists = true;

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances().Subscribe(_pages.Add);

        await _changeStreams.Read();
        _pagesWhilePromoting = _pages.Count;

        _documents = [new BsonDocument("_id", "a")];
        _promotingExists = false;
        _primaryId = Guid.NewGuid();
        await _changeStreams.Read();
    }

    [Fact] void should_emit_nothing_while_promoting() => _pagesWhilePromoting.ShouldEqual(0);
    [Fact] void should_only_read_the_collection_once_the_promotion_is_done() => _collection.Received(1).FindAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<FindOptions<BsonDocument, BsonDocument>>(), Arg.Any<CancellationToken>());
    [Fact] void should_emit_the_page_once_the_rebuilt_collection_is_in_place() => _pages.Count.ShouldEqual(1);
    [Fact] void should_emit_the_documents_of_the_collection() => _pages[0].Count().ShouldEqual(1);
}
