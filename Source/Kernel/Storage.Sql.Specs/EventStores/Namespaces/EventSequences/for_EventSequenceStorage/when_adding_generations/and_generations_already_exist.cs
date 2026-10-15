// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_adding_generations;

public class and_generations_already_exist : given.a_storage_with_stored_generations
{
    string _before;
    string _after;
    bool _added;

    async Task Establish()
    {
        _before = await Fingerprint();
    }

    async Task Because()
    {
        _added = await _storage.TryAddGenerations(_observed, [Target(3), Target(1)]);
        _after = await Fingerprint();
    }

    [Fact] void should_reject_the_whole_write() => _added.ShouldBeFalse();
    [Fact] void should_leave_existing_bytes_unchanged() => _after.ShouldEqual(_before);
}
