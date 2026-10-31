// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_recovery_evidence_is_malformed : given.a_roster_history
{
    void Establish()
    {
        Add(new UserGrantedAdministrator(_first, GrantReason.OperatorRecovery, "not-json"));
        Add(new UserGrantedAdministrator(_second, GrantReason.OperatorRecovery, "{}"));
        Add(new UserGrantedAdministrator(_third, GrantReason.OperatorRecovery, "{\"Nonce\":5}"));
    }

    void Because() => Replay();

    [Fact] void should_not_grant() => _history.Granted.ShouldBeEmpty();
    [Fact] void should_require_recovery() => _history.RecoveryRequired.ShouldBeTrue();
}
