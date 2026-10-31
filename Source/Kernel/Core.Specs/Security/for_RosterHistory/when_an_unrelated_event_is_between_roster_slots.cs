// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_an_unrelated_event_is_between_roster_slots : given.a_roster_history
{
    void Establish()
    {
        Add(new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"));
        var unrelated = AppendedEvent.Empty() with { Context = EventContext.Empty with { SequenceNumber = 1UL, EventSourceId = "unrelated" } };
        _events.Add(unrelated);
        _contents[unrelated] = new object();
        Add(new UserGrantedAdministrator(_second, GrantReason.AdministratorGrant, "spec"));
    }

    void Because() => Replay();

    [Fact] void should_preserve_the_chain_across_global_gaps() => _history.Granted.OrderBy(id => id.ToString()).ShouldEqual(new[] { _first, _second }.OrderBy(id => id.ToString()));
    [Fact] void should_not_require_recovery() => _history.RecoveryRequired.ShouldBeFalse();
}
