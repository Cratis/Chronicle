// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_observing_instances;

/// <summary>
/// A promotion that was cut short leaves the promoting collection behind. Next to an existing primary collection it is
/// not a promotion in progress, so the page is read and emitted - the empty page included.
/// </summary>
public class and_a_promoting_collection_is_left_behind_next_to_the_primary : given.a_sink_observing_a_container
{
    int _emptyPages;

    void Establish()
    {
        _primaryId = Guid.NewGuid();
        _promotingExists = true;
    }

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances().Subscribe(_pages.Add);

        await _changeStreams.Read();
        _emptyPages = _pages.Count(page => !page.Any());

        _documents = [new BsonDocument("_id", "a")];
        await _changeStreams.Read();
    }

    [Fact] void should_emit_the_empty_page() => _emptyPages.ShouldEqual(1);
    [Fact] void should_emit_the_page_of_the_next_read() => _pages.Count.ShouldEqual(2);
    [Fact] void should_emit_the_documents_of_the_collection() => _pages[1].Count().ShouldEqual(1);
}
