// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreNamespaceStorage;

public class when_checking_an_empty_sequence_handle : given.a_namespace_storage
{
    bool _hasData;
    bool _hasState;

    void Establish() => _storage.GetEventSequence(EventSequenceId.Log);

    async Task Because()
    {
        _hasData = await _storage.HasData();
        _hasState = await _storage.HasPersistedState();
    }

    [Fact] void should_have_no_event_data() => _hasData.ShouldBeFalse();
    [Fact] void should_preserve_the_conservative_state_check() => _hasState.ShouldBeTrue();
}
