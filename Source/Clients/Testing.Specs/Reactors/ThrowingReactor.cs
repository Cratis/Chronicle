// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A test reactor whose handler always throws, used to prove <see cref="ReactorScenario{TReactor}"/> surfaces
/// what a reactor handler throws rather than swallowing it.
/// </summary>
public class ThrowingReactor : IReactor
{
    /// <summary>
    /// Reacts to a <see cref="ReservationMade"/> event by always throwing.
    /// </summary>
    /// <param name="event">The triggering <see cref="ReservationMade"/> event.</param>
    /// <returns>A <see cref="Task"/> that always faults with <see cref="ReservationNotYetVisible"/>.</returns>
    /// <exception cref="ReservationNotYetVisible">Always thrown.</exception>
    public async Task ReservationMade(ReservationMade @event)
    {
        await Task.Yield();
        throw new ReservationNotYetVisible();
    }
}
