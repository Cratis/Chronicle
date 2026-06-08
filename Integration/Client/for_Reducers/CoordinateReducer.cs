// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using Cratis.Geospatial;

namespace Cratis.Chronicle.Integration.for_Reducers;

/// <summary>
/// Reducer that folds <see cref="CoordinateEvent"/> into <see cref="CoordinateReadModel"/>.
/// </summary>
[DependencyInjection.IgnoreConvention]
public class CoordinateReducer : IReducerFor<CoordinateReadModel>
{
    /// <summary>
    /// Gets the number of handled events.
    /// </summary>
    public int HandledEvents;

    /// <summary>
    /// Gets the last received <see cref="Coordinate"/>.
    /// </summary>
    public Coordinate LastLocation;

    /// <summary>
    /// Handles a <see cref="CoordinateEvent"/>.
    /// </summary>
    /// <param name="evt">The event.</param>
    /// <param name="input">The current read model.</param>
    /// <param name="ctx">The event context.</param>
    /// <returns>The updated read model.</returns>
    public Task<CoordinateReadModel?> OnCoordinateEvent(CoordinateEvent evt, CoordinateReadModel? input, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        LastLocation = evt.Location;
        return Task.FromResult<CoordinateReadModel?>(new CoordinateReadModel(evt.Location));
    }

    /// <summary>
    /// Waits until the handled event count reaches the specified value.
    /// </summary>
    /// <param name="count">Target event count.</param>
    /// <param name="timeout">Optional timeout.</param>
    /// <returns>A <see cref="Task"/> that completes when the count is reached.</returns>
    public async Task WaitTillHandledEventReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (HandledEvents < count)
        {
            await Task.Delay(50, cts.Token);
        }
    }
}
