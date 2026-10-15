// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents a delivery scheduling ticket.
/// </summary>
/// <param name="Value">The ticket, bounded by <see cref="long.MaxValue"/> before storage.</param>
public sealed record DeliveryTicket(ulong Value) : ConceptAs<ulong>(Value);
