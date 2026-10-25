// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// A bounded page in sequence-number order. Continuation is exclusive of the last returned event.
/// </summary>
/// <param name="Events">The events in read order.</param>
/// <param name="NextSequenceNumber">The next inclusive bound, or null at the end of the matching range.</param>
/// <param name="HasMore">Whether another matching event was found in this read.</param>
public record EventSequencePage(IEnumerable<AppendedEvent> Events, EventSequenceNumber? NextSequenceNumber, bool HasMore);
