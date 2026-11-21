// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreNamespaceStorage;

public class when_checking_after_removing_the_last_event_sequence : given.a_namespace_with_an_event
{
    protected override EventSequenceId Sequence => EventSequenceId.Log;
    bool _hasData;

    async Task Because()
    {
        _storage.RemoveEventSequence(Sequence);
        _hasData = await _storage.HasData();
    }

    [Fact] void should_not_keep_a_sticky_event_presence_flag() => _hasData.ShouldBeFalse();
}
