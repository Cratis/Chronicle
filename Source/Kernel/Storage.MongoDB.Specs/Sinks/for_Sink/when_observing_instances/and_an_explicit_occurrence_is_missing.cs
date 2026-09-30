// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_observing_instances;

/// <summary>
/// An explicit occurrence is not the read model's primary collection, so a replay promotion does not touch it: a
/// missing one is an empty page, emitted without looking at the promoting collection.
/// </summary>
public class and_an_explicit_occurrence_is_missing : given.a_sink_observing_a_container
{
    void Establish() => _promotingExists = true;

    async Task Because()
    {
        using var subscription = _sink.ObserveInstances("Something-revert").Subscribe(_pages.Add);
        await _changeStreams.Read();
    }

    [Fact] void should_emit_one_page() => _pages.Count.ShouldEqual(1);
    [Fact] void should_emit_an_empty_page() => _pages[0].ShouldBeEmpty();
    [Fact] void should_not_look_at_the_collections() => _listCollectionsCalls.ShouldEqual(0);
}
