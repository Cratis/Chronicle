// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Read.EventSequences;

/// <summary>
/// Represents the result of getting appended events.
/// </summary>
/// <param name="Events">The events that was fetched.</param>
/// <param name="TailSequenceNumber">The tail sequence number.</param>
public record AppendedEvents(IEnumerable<AppendedEventWithJsonAsContent> Events, EventSequenceNumber TailSequenceNumber);
