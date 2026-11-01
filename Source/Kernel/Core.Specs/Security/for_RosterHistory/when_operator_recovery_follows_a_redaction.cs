// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_operator_recovery_follows_a_redaction : given.a_roster_history
{
    void Establish()
    {
        Add(new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"));
        Redact(Add(new UserRevokedAdministrator(_first, "spec")));
        Add(new UserGrantedAdministrator(_second, GrantReason.OperatorRecovery, "{\"Nonce\":\"fresh\"}"));
    }

    void Because() => Replay();

    [Fact] void should_grant_only_the_recovery_user() => _history.Granted.ShouldContainOnly(_second);
}
