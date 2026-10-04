// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_Reducers.when_replaying;

/// <summary>
/// A reducer whose weighting can change between runs - standing in for a reducer whose code changed between releases,
/// which is what a full replay exists to apply to the read models it already built.
/// </summary>
[DependencyInjection.IgnoreConvention]
public class WeightingReducer : IReducerFor<ReplayedTotal>
{
    /// <summary>
    /// Gets or sets the weight each number is multiplied by.
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// Gets or sets the event source the reducer fails for, standing in for a reducer that throws on some partition.
    /// </summary>
    public EventSourceId? FailFor { get; set; }

    /// <summary>
    /// Adds a weighted number to the total.
    /// </summary>
    /// <param name="event">The number that was added.</param>
    /// <param name="current">The current total.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The new total.</returns>
    /// <exception cref="InvalidOperationException">The event belongs to the event source the reducer fails for.</exception>
    public ReplayedTotal Added(ReplayNumberAdded @event, ReplayedTotal? current, EventContext context)
    {
        if (FailFor is not null && context.EventSourceId == FailFor)
        {
            throw new InvalidOperationException($"Failing deliberately for '{context.EventSourceId}'");
        }

        return new((current?.Total ?? 0) + (@event.Number * Weight));
    }
}
