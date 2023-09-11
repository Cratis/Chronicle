// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.New;

/// <summary>
/// Defines a system that can handle events from an <see cref="IObserver"/>.
/// </summary>
public interface IObserverEventHandler
{
    /// <summary>
    /// Handle an event.
    /// </summary>
    /// <param name="observerId">The identifier of the observer that should handle it.</param>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> it is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> it is for.</param>
    /// <param name="event">The <see cref="AppendedEvent"/> to handle.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(ObserverId observerId, MicroserviceId microserviceId, TenantId tenantId, AppendedEvent @event);
}
