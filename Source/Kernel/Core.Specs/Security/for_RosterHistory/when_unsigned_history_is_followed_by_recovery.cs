// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_unsigned_history_is_followed_by_recovery : given.a_roster_history
{
    void Establish()
    {
        Add(new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"), omitProof: true);
        Add(new UserGrantedAdministrator(_second, GrantReason.Bootstrap, "spec"));
        Add(new UserGrantedAdministrator(_third, GrantReason.OperatorRecovery, "{\"Nonce\":\"fresh\"}"));
        Add(new UserGrantedAdministrator(_second, GrantReason.UpgradeProvenance, "spec"));
    }

    void Because() => Replay();

    [Fact] void should_grant_only_operator_recovery() => _history.Granted.ShouldContainOnly(_third);
    [Fact] void should_keep_automatic_grants_disabled() => _history.Compromised.ShouldBeTrue();
}
