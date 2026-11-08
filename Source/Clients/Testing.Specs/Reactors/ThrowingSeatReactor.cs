// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A reactor whose batch method always throws.
/// </summary>
public class ThrowingSeatReactor : IReactor
{
    /// <summary>
    /// Fails every partition of the batch by throwing.
    /// </summary>
    /// <param name="batch">The <see cref="EventBatch"/>.</param>
    /// <returns>A task that always faults with <see cref="SeatUnavailable"/>.</returns>
    /// <exception cref="SeatUnavailable">Always thrown.</exception>
    [Observes<SeatReleased>]
    public Task Index(EventBatch batch) => Task.FromException(new SeatUnavailable("every"));
}
