// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreNamespaceStorage;

public class when_checking_a_custom_sequence_event : given.a_namespace_with_an_event
{
    protected override EventSequenceId Sequence => new("custom-events");
    bool _hasData;

    async Task Because() => _hasData = await _storage.HasData();

    [Fact] void should_find_events_outside_the_log() => _hasData.ShouldBeTrue();
}
