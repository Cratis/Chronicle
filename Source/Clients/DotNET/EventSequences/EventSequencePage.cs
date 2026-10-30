// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// A bounded page of events in read order.
/// </summary>
/// <param name="Events">The events, oldest first for forward reads or newest first for backward reads.</param>
/// <param name="Next">The next inclusive start in the read direction, or null when no matching events remain within the bounds.</param>
public record EventSequencePage(IImmutableList<AppendedEvent> Events, EventSequenceNumber? Next)
{
    /// <summary>
    /// Gets whether another page of matching events exists within the requested bounds.
    /// </summary>
    public bool HasMore => Next is not null;
}
