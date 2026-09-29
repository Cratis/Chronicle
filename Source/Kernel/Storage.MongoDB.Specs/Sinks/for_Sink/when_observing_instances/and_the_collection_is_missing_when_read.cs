// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_observing_instances;

/// <summary>
/// A read that runs while a replay promotion has the primary collection renamed aside finds no collection. That is
/// not an empty read model, so nothing is emitted; the read that follows the rename into place emits the page.
/// </summary>
public class and_the_collection_is_missing_when_read : given.a_sink_observing_a_container
{
    int _pagesWhileMissing;

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances().Subscribe(_pages.Add);

        await _changeStreams.Read();
        _pagesWhileMissing = _pages.Count;

        _documents = [new BsonDocument("_id", "a")];
        _collectionExists = true;
        await _changeStreams.Read();
    }

    [Fact] void should_emit_nothing_while_the_collection_is_missing() => _pagesWhileMissing.ShouldEqual(0);
    [Fact] void should_emit_the_page_once_the_collection_is_back() => _pages.Count.ShouldEqual(1);
    [Fact] void should_emit_the_documents_of_the_collection() => _pages[0].Count().ShouldEqual(1);
}
