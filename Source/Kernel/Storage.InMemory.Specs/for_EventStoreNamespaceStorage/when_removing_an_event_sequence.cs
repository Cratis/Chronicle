// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreNamespaceStorage;

public class when_removing_an_event_sequence : given.a_namespace_storage
{
    static readonly EventSequenceId _scratch = new("scratch");
    static readonly EventSequenceId _kept = new("kept");

    IEventSequenceStorage _keptBefore;
    IUniqueConstraintsStorage _scratchConstraintsBefore;
    IEnumerable<EventSequenceId> _sequences;

    void Establish()
    {
        _storage.GetEventSequence(_scratch);
        _keptBefore = _storage.GetEventSequence(_kept);
        _scratchConstraintsBefore = _storage.GetUniqueConstraintsStorage(_scratch);
    }

    async Task Because()
    {
        _storage.RemoveEventSequence(_scratch);
        _sequences = await _storage.GetEventSequences();
    }

    [Fact] void should_only_list_the_kept_sequence() => _sequences.ShouldContainOnly(_kept);
    [Fact] void should_keep_the_other_sequence_as_is() => _storage.GetEventSequence(_kept).ShouldBeSame(_keptBefore);
    [Fact] void should_start_the_removed_sequence_afresh_when_asked_again() => _storage.GetUniqueConstraintsStorage(_scratch).ShouldNotBeSame(_scratchConstraintsBefore);
}
