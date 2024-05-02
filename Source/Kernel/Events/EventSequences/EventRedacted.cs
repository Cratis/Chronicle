// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;

namespace Cratis.Kernel.Events.EventSequences;

/// <summary>
/// The event that occurs when an event is redacted.
/// </summary>
/// <param name="Sequence"><see cref="EventSequenceId"/> the event was in.</param>
/// <param name="SequenceNumber"><see cref="EventSequenceNumber"/> of the event that was redacted.</param>
/// <param name="Reason"><see cref="RedactionReason"/> representing why it was redacted.</param>
public record EventRedacted(
    EventSequenceId Sequence,
    EventSequenceNumber SequenceNumber,
    RedactionReason Reason);
