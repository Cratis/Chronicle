// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

/// <summary>
/// A bare <see cref="Guid"/> child key on the same event source as the parent, with no explicit parentKey.
/// Parent-key inference excludes the child key, finds no other candidate, and falls back to the event
/// source — so the children resolve. (Before the discovery fix this deferred forever.)
/// </summary>
public class and_child_keyed_by_guid_on_the_same_event_source : Specification
{
    ReadModelScenario<SameSourceTicketLedger> _scenario;
    EventSourceId _ledgerId;
    Guid _firstTicket;
    Guid _secondTicket;

    void Establish()
    {
        _scenario = new ReadModelScenario<SameSourceTicketLedger>();
        _ledgerId = new EventSourceId(Guid.NewGuid());
        _firstTicket = Guid.NewGuid();
        _secondTicket = Guid.NewGuid();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_ledgerId)
            .Events(
                new LedgerOpened("Operations"),
                new TicketLogged(_firstTicket, 100m),
                new TicketLogged(_secondTicket, 200m));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_ticket_lines() => _scenario.Instance!.Tickets.Count().ShouldEqual(2);
    [Fact] void should_map_first_ticket_key() => _scenario.Instance!.Tickets.First().Ticket.ShouldEqual(_firstTicket);
    [Fact] void should_map_first_ticket_amount() => _scenario.Instance!.Tickets.First().Amount.ShouldEqual(100m);
}
