// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_a_revocation_key_is_unavailable : given.a_roster_history
{
    void Establish()
    {
        Add(new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"));
        Add(new UserRevokedAdministrator(_first, "spec"), _otherProof);
    }

    void Because() => Replay();

    [Fact] void should_suspend_all_privileges() => _history.Granted.ShouldBeEmpty();
    [Fact] void should_require_recovery() => _history.RecoveryRequired.ShouldBeTrue();
    [Fact] void should_keep_the_raw_tail() => _history.RawRosterTail.ShouldEqual(_events[^1].Context.SequenceNumber);
}
