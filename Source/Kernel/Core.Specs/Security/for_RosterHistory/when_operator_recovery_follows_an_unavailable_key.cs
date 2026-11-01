// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_operator_recovery_follows_an_unavailable_key : given.a_roster_history
{
    void Establish()
    {
        Add(new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"));
        Add(new UserRevokedAdministrator(_first, "spec"), _otherProof);
        Add(new UserGrantedAdministrator(_third, GrantReason.OperatorRecovery, "{\"Nonce\":\"fresh\"}"));
    }

    void Because() => Replay();

    [Fact] void should_grant_only_the_recovery_user() => _history.Granted.ShouldContainOnly(_third);
    [Fact] void should_finish_recovery() => _history.RecoveryRequired.ShouldBeFalse();
    [Fact] void should_keep_automatic_grants_disabled() => _history.Compromised.ShouldBeTrue();
}
