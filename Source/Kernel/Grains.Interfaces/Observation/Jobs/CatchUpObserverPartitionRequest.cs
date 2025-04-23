// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents the request for a <see cref="ICatchUpObserverPartition"/>.
/// </summary>
/// <param name="ObserverKey">The additional <see cref="ObserverKey"/> for the observer to catch up.</param>
/// <param name="ObserverType">The <see cref="ObserverType"/>.</param>
/// <param name="Key"><see cref="Key">Partition</see> to retry.</param>
/// <param name="FromSequenceNumber">From <see cref="EventSequenceNumber"/> to catch up from.</param>
/// <param name="EventTypes">The event types.</param>
public record CatchUpObserverPartitionRequest(
    ObserverKey ObserverKey,
    ObserverType ObserverType,
    Key Key,
    EventSequenceNumber FromSequenceNumber,
    IEnumerable<EventType> EventTypes) : ObserverPartitionedJobRequest(ObserverKey, ObserverType, Key);
