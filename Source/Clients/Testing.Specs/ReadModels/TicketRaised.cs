// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event recording a ticket against a ledger, keyed by a bare (non-concept) <see cref="Guid"/>.
/// The ticket is appended to its own event source; <paramref name="LedgerId"/> links it to the parent.
/// </summary>
/// <param name="LedgerId">The parent ledger identifier (used as the parent key).</param>
/// <param name="Ticket">The ticket identifier, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
[EventType]
public record TicketRaised(Guid LedgerId, Guid Ticket, decimal Amount);
