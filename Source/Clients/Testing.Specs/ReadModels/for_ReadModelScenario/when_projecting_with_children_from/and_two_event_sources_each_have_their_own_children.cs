// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

/// <summary>
/// Two roots seeded in one scenario, each with a child of its own on its own event source and no explicit
/// <c>parentKey</c> — so the event source id is the only thing routing a child to its parent. Each instance
/// must carry only its own child, exactly as the kernel does; the harness previously let the later document
/// accumulate the earlier one's children because every event shared one threaded state.
/// </summary>
public class and_two_event_sources_each_have_their_own_children : Specification
{
    ReadModelScenario<SameSourceTicketLedger> _scenario;
    EventSourceId _firstLedgerId;
    EventSourceId _secondLedgerId;
    Guid _firstTicket;
    Guid _secondTicket;

    void Establish()
    {
        _scenario = new ReadModelScenario<SameSourceTicketLedger>();
        _firstLedgerId = new EventSourceId(Guid.NewGuid());
        _secondLedgerId = new EventSourceId(Guid.NewGuid());
        _firstTicket = Guid.NewGuid();
        _secondTicket = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_firstLedgerId)
            .Events(new LedgerOpened("First"), new TicketLogged(_firstTicket, 100m));

        await _scenario.Given
            .ForEventSource(_secondLedgerId)
            .Events(new LedgerOpened("Second"), new TicketLogged(_secondTicket, 200m));
    }

    [Fact] void should_materialize_both_ledgers() => _scenario.Instances.Count.ShouldEqual(2);
    [Fact] void should_name_the_first_ledger() => _scenario.InstanceForEventSourceId(_firstLedgerId)!.Name.ShouldEqual("First");
    [Fact] void should_name_the_second_ledger() => _scenario.InstanceForEventSourceId(_secondLedgerId)!.Name.ShouldEqual("Second");
    [Fact] void should_give_the_first_ledger_only_its_own_ticket() => _scenario.InstanceForEventSourceId(_firstLedgerId)!.Tickets.Select(_ => _.Ticket).ShouldContainOnly(_firstTicket);
    [Fact] void should_give_the_second_ledger_only_its_own_ticket() => _scenario.InstanceForEventSourceId(_secondLedgerId)!.Tickets.Select(_ => _.Ticket).ShouldContainOnly(_secondTicket);
}
