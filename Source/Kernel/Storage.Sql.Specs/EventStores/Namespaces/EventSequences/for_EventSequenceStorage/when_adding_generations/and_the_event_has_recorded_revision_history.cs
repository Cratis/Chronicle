// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_adding_generations;

public class and_the_event_has_recorded_revision_history : given.a_storage_with_stored_generations
{
    string? _before;
    string? _after;
    bool _added;

    async Task Establish()
    {
        await Revise();
        _observed = await _storage.GetStoredGenerations(0);
        _before = Stored(0).Revisions;
    }
    async Task Because()
    {
        _added = await _storage.TryAddGenerations(_observed, [Target()]);
        _after = Stored(0).Revisions;
    }

    [Fact] void should_add_the_generation_using_the_existing_token() => _added.ShouldBeTrue();
    [Fact] void should_keep_the_revision_history_unchanged() => _after.ShouldEqual(_before);
    [Fact] void should_keep_the_original_envelope() => Stored(0).OriginalContent.ShouldNotBeNull();
}
