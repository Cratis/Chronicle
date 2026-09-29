// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_observing_instances;

/// <summary>
/// MongoDB creates a collection on its first write, so a read model nothing has been written to has no collection
/// at all. That is an empty read model, and the initial empty page is emitted.
/// </summary>
public class and_the_collection_was_never_created : given.a_sink_observing_a_container
{
    async Task Because()
    {
        using var subscription = _sink.ObserveInstances().Subscribe(_pages.Add);
        await _changeStreams.Read();
    }

    [Fact] void should_emit_one_page() => _pages.Count.ShouldEqual(1);
    [Fact] void should_emit_an_empty_page() => _pages[0].ShouldBeEmpty();
}
