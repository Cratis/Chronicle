// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreNamespaceStorage;

public class when_only_occurrence_state_exists : given.a_namespace_storage
{
    bool _hasData;
    bool _hasState;

    async Task Establish() => await _storage.Occurrences.Record(new(
        new OccurrenceId("occurrence"),
        new TriggerId("trigger"),
        OccurrenceKind.Application,
        "fingerprint",
        "{}",
        1,
        DateTimeOffset.UtcNow,
        null,
        null,
        OccurrenceState.Pending,
        0,
        0,
        null,
        null,
        null,
        null));

    async Task Because()
    {
        _hasData = await _storage.HasData();
        _hasState = await _storage.HasPersistedState();
    }

    [Fact] void should_report_no_event_data() => _hasData.ShouldBeFalse();
    [Fact] void should_preserve_the_recoverable_state_check() => _hasState.ShouldBeTrue();
}
