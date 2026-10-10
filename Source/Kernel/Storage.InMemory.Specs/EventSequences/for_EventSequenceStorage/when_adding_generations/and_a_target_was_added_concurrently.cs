// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_adding_generations;

public class and_a_target_was_added_concurrently : given.a_storage_with_stored_generations
{
    string _before;
    string _after;
    bool _added;

    async Task Establish()
    {
        (await _storage.TryAddGenerations(_observed, [Target()])).ShouldBeTrue();
        _before = await Fingerprint();
    }

    async Task Because()
    {
        _added = await _storage.TryAddGenerations(_observed, [Target()]);
        _after = await Fingerprint();
    }

    [Fact] void should_reject_the_whole_write() => _added.ShouldBeFalse();
    [Fact] void should_leave_existing_bytes_unchanged() => _after.ShouldEqual(_before);
}
