// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

/// <summary>
/// A reactor that throws before returning a task.
/// </summary>
public class SynchronouslyThrowingReactor : IReactor
{
    /// <summary>
    /// Rejects a reservation synchronously.
    /// </summary>
    /// <param name="event">The triggering event.</param>
    /// <returns>A task that is never returned because the handler throws.</returns>
    /// <exception cref="ReservationNotYetVisible">Always thrown before returning a task.</exception>
    public Task Handle(ReservationMade @event) => throw new ReservationNotYetVisible();
}
