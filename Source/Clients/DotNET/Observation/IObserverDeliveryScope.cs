// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a hook that is entered for every delivery before its service scope is created.
/// </summary>
/// <remarks>
/// Implementations are resolved from the root service provider for every delivery, and all registrations are entered
/// in registration order. Register them as singleton or transient. <see cref="Enter"/> runs synchronously, so
/// <see cref="AsyncLocal{T}"/> values it sets are visible to the delivery. An exception thrown by
/// <see cref="Enter"/> fails the delivery. The returned disposable is disposed after the delivery's service scope.
/// </remarks>
public interface IObserverDeliveryScope
{
    /// <summary>
    /// Enters the scope for a delivery.
    /// </summary>
    /// <param name="context">The <see cref="ObserverDeliveryContext"/> describing the delivery.</param>
    /// <returns>A disposable that leaves the scope.</returns>
    IDisposable Enter(ObserverDeliveryContext context);
}
