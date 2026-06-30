// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event recording a ticket on the same event source as its parent ledger, carrying only the
/// bare (non-concept) <see cref="Guid"/> child key and no parent reference.
/// </summary>
/// <param name="Ticket">The ticket identifier, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
[EventType]
public record TicketLogged(Guid Ticket, decimal Amount);
