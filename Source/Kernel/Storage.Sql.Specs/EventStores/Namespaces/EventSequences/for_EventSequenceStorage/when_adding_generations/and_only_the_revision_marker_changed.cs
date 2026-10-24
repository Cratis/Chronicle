// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_adding_generations;

public class and_only_the_revision_marker_changed : given.a_storage_with_stored_generations
{
    string _before;
    string _afterRevision;
    string _after;
    bool _added;

    async Task Establish()
    {
        await Revise();
        _observed = (await _storage.GetStoredGenerations(0))!;
        _before = await Fingerprint();
        await _storage.Revise(0, _eventType with { Generation = new EventTypeGeneration(2) }, CorrelationId.NotSet, [], [], DateTimeOffset.UtcNow, Content()[new EventTypeGeneration(2)], await Hash(2));
        _afterRevision = await Fingerprint();
    }

    async Task Because()
    {
        _added = await _storage.TryAddGenerations(_observed, [Target(sourceIsAppended: false)]);
        _after = await Fingerprint();
    }

    [Fact] void should_leave_content_hashes_and_provenance_unchanged_when_revising() => _afterRevision.ShouldEqual(_before);
    [Fact] void should_reject_the_write() => _added.ShouldBeFalse();
    [Fact] void should_preserve_content_hashes_and_provenance() => _after.ShouldEqual(_before);
    [Fact] void should_preserve_the_new_revision_marker() => Stored(0).RevisedGeneration.ShouldEqual((uint?)2);
}
