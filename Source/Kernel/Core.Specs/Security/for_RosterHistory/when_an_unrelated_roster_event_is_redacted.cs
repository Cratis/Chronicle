// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_an_unrelated_roster_event_is_redacted : given.a_roster_history
{
    AppendedEvent _grant;

    void Establish()
    {
        var first = Add(new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"));
        AddRedactedUnrelatedEvent();
        var grant = WithProof(new UserGrantedAdministrator(_second, GrantReason.AdministratorGrant, "spec"), Roster.EventSourceId, first.Context.SequenceNumber, _proof);
        _grant = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with { EventType = grant.GetType().GetEventType(), EventSourceId = Roster.EventSourceId, SequenceNumber = (ulong)_events.Count }
        };
        _events.Add(_grant);
        _contents.Add(_grant, grant);
        AddRedactedUnrelatedEvent();
    }

    void Because() => Replay();

    [Fact] void should_verify_signatures_chained_over_the_tombstone() => _history.Granted.OrderBy(id => id.ToString()).ShouldEqual(new[] { _first, _second }.OrderBy(id => id.ToString()));
    [Fact] void should_not_require_recovery() => _history.RecoveryRequired.ShouldBeFalse();
    [Fact] void should_keep_the_proof_predecessor_on_the_last_family_slot() => _history.RawRosterTail.ShouldEqual(_grant.Context.SequenceNumber);
    [Fact] void should_advance_the_concurrency_tail_to_the_last_tombstone() => _history.RosterScopeTail.ShouldEqual(_events[^1].Context.SequenceNumber);

    void AddRedactedUnrelatedEvent()
    {
        var unrelated = AppendedEvent.Empty() with
        {
            Context = EventContext.Empty with { EventType = typeof(ApplicationRemoved).GetEventType(), EventSourceId = Roster.EventSourceId, SequenceNumber = (ulong)_events.Count }
        };
        _events.Add(unrelated);
        _contents.Add(unrelated, new ApplicationRemoved());
        Redact(unrelated);
    }
}
