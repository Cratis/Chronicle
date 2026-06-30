// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a child collection keyed by a bare (non-concept) <see cref="Guid"/> on the SAME event
/// source as the parent, with no explicit <c>parentKey</c>. Because the child key is excluded from
/// parent-key inference, this resolves to the event source — exercising the parent-key discovery fix.
/// </summary>
/// <param name="Id">Ledger identifier.</param>
/// <param name="Name">The ledger name.</param>
/// <param name="Tickets">Ticket lines keyed by <see cref="TicketLogged.Ticket"/> (a bare <see cref="Guid"/>).</param>
[Passive]
[FromEvent<LedgerOpened>]
public record SameSourceTicketLedger(
    Guid Id,
    string Name,

    [ChildrenFrom<TicketLogged>(key: nameof(TicketLogged.Ticket), identifiedBy: nameof(TicketLine.Ticket))]
    IEnumerable<TicketLine> Tickets);
