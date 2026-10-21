// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// The exception that is thrown when a redacted event lacks the original type needed to interpret closing transitions.
/// </summary>
/// <param name="sequenceNumber">The malformed redacted event position.</param>
public class RedactedClosingEventTypeUnavailable(EventSequenceNumber sequenceNumber) : Exception($"Redacted event at sequence number '{sequenceNumber}' has no original event type.");
