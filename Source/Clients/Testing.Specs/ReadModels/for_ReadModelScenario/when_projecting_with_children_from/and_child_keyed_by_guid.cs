// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_child_keyed_by_guid : Specification
{
    ReadModelScenario<TicketLedger> _scenario;
    EventSourceId _ledgerId;
    Guid _ledgerGuid;
    Guid _firstTicket;
    Guid _secondTicket;

    void Establish()
    {
        _scenario = new ReadModelScenario<TicketLedger>();
        _ledgerGuid = Guid.NewGuid();
        _ledgerId = new EventSourceId(_ledgerGuid);
        _firstTicket = Guid.NewGuid();
        _secondTicket = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_ledgerId)
            .Events(new LedgerOpened("Operations"));

        await _scenario.Given
            .ForEventSource(_firstTicket)
            .Events(new TicketRaised(_ledgerGuid, _firstTicket, 100m));

        await _scenario.Given
            .ForEventSource(_secondTicket)
            .Events(new TicketRaised(_ledgerGuid, _secondTicket, 200m));
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_ticket_lines() => _scenario.Instance!.Tickets.Count().ShouldEqual(2);
    [Fact] void should_map_first_ticket_key() => _scenario.Instance!.Tickets.First().Ticket.ShouldEqual(_firstTicket);
    [Fact] void should_map_first_ticket_amount() => _scenario.Instance!.Tickets.First().Amount.ShouldEqual(100m);
    [Fact] void should_map_second_ticket_key() => _scenario.Instance!.Tickets.Last().Ticket.ShouldEqual(_secondTicket);
}
