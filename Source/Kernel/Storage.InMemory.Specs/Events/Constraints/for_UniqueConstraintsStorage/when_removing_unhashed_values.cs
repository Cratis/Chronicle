// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_UniqueConstraintsStorage;

public class when_removing_unhashed_values : given.a_unique_constraints_storage
{
    (long PerEventSource, long PerValue) _counts;

    async Task Establish() => await _storage.Save("owner", _definition, 1, "unhashed-in-memory-value");

    async Task Because() => _counts = await _storage.RemoveUnhashedValues();

    [Fact] void should_report_no_persisted_legacy_claims() => _counts.ShouldEqual((0L, 0L));
    [Fact] async Task should_not_remove_current_in_memory_claims() => (await _storage.IsAllowed("other", _definition, "unhashed-in-memory-value")).IsAllowed.ShouldBeFalse();
}
