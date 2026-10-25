// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Attribute used to declare how the events of a reactor or reducer are delivered, overriding the default of the
/// current serverless mode.
/// </summary>
/// <remarks>
/// A configuration override under <c language="json">Cratis:Chronicle:Serverless:Observers:&lt;ObserverId&gt;:Delivery</c> takes
/// precedence over this attribute. Without either, the observer uses <see cref="ObserverDelivery.Stream"/> when
/// serverless mode is off and the configured default delivery when it is on.
/// </remarks>
/// <param name="delivery">The <see cref="ObserverDelivery"/> to use for the observer.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class DeliveryAttribute(ObserverDelivery delivery) : Attribute
{
    /// <summary>
    /// Gets the <see cref="ObserverDelivery"/> declared for the observer.
    /// </summary>
    public ObserverDelivery Delivery { get; } = delivery;
}
