// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child line keyed by a bare (non-concept) <see cref="Guid"/> ticket identifier.
/// </summary>
/// <param name="Ticket">The ticket identifier, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
public record TicketLine(
    [Key] Guid Ticket,
    decimal Amount);
