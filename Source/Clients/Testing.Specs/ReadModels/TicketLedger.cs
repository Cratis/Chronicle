// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a child collection keyed by a bare (non-concept) <see cref="Guid"/>, used to verify
/// the in-memory harness materializes <see cref="Guid"/>-keyed child collections. A bare <see cref="Guid"/>
/// child key is treated by Chronicle as an event-source reference, so — matching the real runtime — the
/// children are appended to their own event source and linked to the parent via an explicit
/// <see cref="ChildrenFromAttribute{TEvent}.ParentKey"/>.
/// </summary>
/// <param name="Id">Ledger identifier.</param>
/// <param name="Name">The ledger name.</param>
/// <param name="Tickets">Ticket lines keyed by <see cref="TicketRaised.Ticket"/> (a bare <see cref="Guid"/>).</param>
[Passive]
[FromEvent<LedgerOpened>]
public record TicketLedger(
    Guid Id,
    string Name,

    [ChildrenFrom<TicketRaised>(
        key: nameof(TicketRaised.Ticket),
        identifiedBy: nameof(TicketLine.Ticket),
        parentKey: nameof(TicketRaised.LedgerId))]
    IEnumerable<TicketLine> Tickets);
