// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents the arguments passed along to a job step representing a specific key on an observer.
/// </summary>
/// <param name="ObserverId"><see cref="ObserverId"/> for the observer.</param>
/// <param name="ObserverKey">The <see cref="ObserverKey"/> with extended details about the observer.</param>
/// <param name="Partition">The partition in the form a <see cref="Key"/>.</param>
/// <param name="EventTypes">The event types that are to replay.</param>
public record ReplayStepRequest(
    ObserverId ObserverId,
    ObserverKey ObserverKey,
    Key Partition,
    IEnumerable<EventType> EventTypes);
