// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_large_number_of_events;

public class and_a_child_collection_grows_with_every_event : Specification
{
    const int NumberOfTickets = 1000;

    ReadModelScenario<TicketLedger> _scenario;
    EventSourceId _ledgerId;
    Guid _ledgerGuid;
    Guid[] _ticketIds;

    void Establish()
    {
        _scenario = new ReadModelScenario<TicketLedger>();
        _ledgerGuid = Guid.NewGuid();
        _ledgerId = new EventSourceId(_ledgerGuid);
        _ticketIds = [.. Enumerable.Range(0, NumberOfTickets).Select(_ => Guid.NewGuid())];
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_ledgerId)
            .Events(new LedgerOpened("Operations"));

        for (var index = 0; index < NumberOfTickets; index++)
        {
            await _scenario.Given
                .ForEventSource(_ticketIds[index])
                .Events(new TicketRaised(_ledgerGuid, _ticketIds[index], index));
        }
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_a_line_per_seeded_ticket() => _scenario.Instance!.Tickets.Count().ShouldEqual(NumberOfTickets);
    [Fact] void should_keep_the_children_in_seed_order() => _scenario.Instance!.Tickets.Select(_ => _.Ticket).ShouldEqual(_ticketIds);
    [Fact] void should_map_every_child_amount() => _scenario.Instance!.Tickets.Select(_ => _.Amount).ShouldEqual(Enumerable.Range(0, NumberOfTickets).Select(_ => (decimal)_));
}
