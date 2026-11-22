// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>Represents the observed tail, applicability decision and retained events of a partition run.</summary>
/// <param name="ObservedTail">The sequence number of the last input event, not the maximum sequence number.</param>
/// <param name="IsApplicable">Whether the entire run satisfies the subscription and independent run-wide filters.</param>
/// <param name="EventsToHandle">The original events at or above the floor, or an empty array for an inapplicable run.</param>
internal readonly record struct PreparedObserverRun(
    EventSequenceNumber ObservedTail,
    bool IsApplicable,
    AppendedEvent[] EventsToHandle);
