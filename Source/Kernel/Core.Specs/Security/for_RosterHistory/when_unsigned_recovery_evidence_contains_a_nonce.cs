// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_unsigned_recovery_evidence_contains_a_nonce : given.a_roster_history
{
    bool _blocked;

    void Establish()
    {
        Add(new UserGrantedAdministrator(_first, GrantReason.OperatorRecovery, "{\"Nonce\":\"used\"}"), unsigned: true);
        Add(new UserGrantedAdministrator(_second, GrantReason.OperatorRecovery, "{\"Nonce\":\"used\"}"));
        Replay();
        _blocked = _history.Granted.Count == 0 && _history.RecoveryNonces.Contains("used");
        Add(new UserGrantedAdministrator(_third, GrantReason.OperatorRecovery, "{\"Nonce\":\"fresh\"}"));
    }

    void Because() => _history.Apply(_events.Last(), appended => _contents[appended]);

    [Fact] void should_consume_an_observed_nonce_without_trusting_the_grant() => _blocked.ShouldBeTrue();
    [Fact] void should_allow_a_fresh_nonce() => _history.Granted.ShouldContainOnly(_third);
}
