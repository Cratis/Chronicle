// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_observing_instances;

/// <summary>
/// A page with instances in it is emitted as read, and costs one look at the collections rather than two.
/// </summary>
public class and_the_page_has_instances : given.a_sink_observing_a_container
{
    void Establish()
    {
        _primaryId = Guid.NewGuid();
        _documents = [new BsonDocument("_id", "a")];
    }

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances().Subscribe(_pages.Add);
        await _changeStreams.Read();
    }

    [Fact] void should_emit_one_page() => _pages.Count.ShouldEqual(1);
    [Fact] void should_emit_the_instances() => _pages[0].Count().ShouldEqual(1);
    [Fact] void should_only_check_the_collections_before_the_read() => _listCollectionsCalls.ShouldEqual(1);
}
