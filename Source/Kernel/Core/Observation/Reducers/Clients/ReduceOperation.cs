// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Represents the information needed to reduce events.
/// </summary>
/// <param name="Partition"><see cref="Key"/> for the partition.</param>
/// <param name="Events">Collection of <see cref="AppendedEvent"/> to reduce from.</param>
/// <param name="InitialState">The initial state to reduce from.</param>
public record ReduceOperation(Key Partition, IEnumerable<AppendedEvent> Events, ExpandoObject? InitialState)
{
    /// <summary>
    /// Gets how long the kernel waits for the result, or null when it waits without a deadline.
    /// </summary>
    public TimeSpan? DeliverWithin { get; init; }

    /// <summary>
    /// Gets the <see cref="ObservationOrigin"/> the events came from.
    /// </summary>
    public ObservationOrigin Origin { get; init; }
}
