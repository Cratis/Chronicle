// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelTableNameBindings.when_transitioning;

public class and_the_replay_attempt_is_compared : given.a_sqlite_ledger
{
    bool _set;
    bool _setAgain;
    bool _cleared;
    ReadModelTableNameBinding _stored;

    async Task Establish() => await Reserve(_owner, "orders");

    async Task Because()
    {
        _set = await _ledger.SetReplayAttempt(_owner.Id, null, "attempt-1");
        _setAgain = await _ledger.SetReplayAttempt(_owner.Id, null, "attempt-2");
        _cleared = await _ledger.SetReplayAttempt(_owner.Id, "attempt-1", null);
        _stored = await CreateLedger(CreateContext()).Find(_owner.Id);
    }

    [Fact] void should_set_when_none_is_present() => _set.ShouldBeTrue();
    [Fact] void should_not_overwrite_a_present_attempt() => _setAgain.ShouldBeFalse();
    [Fact] void should_clear_the_matching_attempt() => _cleared.ShouldBeTrue();
    [Fact] void should_leave_no_attempt() => _stored.ReplayAttempt.ShouldBeNull();
}
