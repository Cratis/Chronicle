// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_a_recovery_nonce_has_already_been_consumed : given.a_roster_history
{
    void Establish()
    {
        Add(new UserGrantedAdministrator(_first, GrantReason.OperatorRecovery, "{\"Nonce\":\"used\"}"));
        Add(new UserRevokedAdministrator(_first, "spec"));
        Add(new UserGrantedAdministrator(_second, GrantReason.OperatorRecovery, "{\"Nonce\":\"used\"}"));
    }

    void Because() => Replay();

    [Fact] void should_not_grant_again() => _history.Granted.ShouldBeEmpty();
}
