// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_an_unsigned_removal_follows_a_grant : given.a_roster_history
{
    void Establish()
    {
        Add(new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"));
        var removal = AppendedEvent.Empty() with { Context = EventContext.Empty with { EventSourceId = _first, SequenceNumber = 1UL, EventType = typeof(UserRemoved).GetEventType() } };
        _events.Add(removal);
        _contents[removal] = new UserRemoved();
    }

    void Because() => Replay();

    [Fact] void should_remove_but_never_grant() => _history.Granted.ShouldBeEmpty();
    [Fact] void should_record_the_removal() => _history.Removed.ShouldContain(_first);
}
