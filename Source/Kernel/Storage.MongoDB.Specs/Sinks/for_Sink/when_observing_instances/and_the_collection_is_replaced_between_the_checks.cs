// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_observing_instances;

/// <summary>
/// A promotion that renames the primary collection aside and then a rebuilt one into its place while the page is read
/// can leave an empty read behind a collection that is now a different one. That empty read says nothing about the
/// read model, so nothing is emitted; the rename into place triggers the read that follows.
/// </summary>
public class and_the_collection_is_replaced_between_the_checks : given.a_sink_observing_a_container
{
    void Establish()
    {
        _primaryId = Guid.NewGuid();
        _duringFind = () => _primaryId = Guid.NewGuid();
    }

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances().Subscribe(_pages.Add);
        await _changeStreams.Read();
    }

    [Fact] void should_check_the_collections_before_and_after_the_read() => _listCollectionsCalls.ShouldEqual(2);
    [Fact] void should_emit_nothing() => _pages.ShouldBeEmpty();
}
