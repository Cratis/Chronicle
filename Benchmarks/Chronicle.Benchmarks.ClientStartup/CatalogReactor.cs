// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Benchmarks.ClientStartup;

/// <summary>
/// Exercises the real reactor invocation path with a run-local dependency.
/// </summary>
/// <param name="probe">The current run's delivery recorder.</param>
public class CatalogReactor(CatalogDeliveryProbe probe) : IReactor
{
    /// <summary>
    /// Records a delivered catalog event.
    /// </summary>
    /// <param name="event">The input event.</param>
    public void Handle(CatalogRecorded @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        probe.Deliveries++;
    }
}
