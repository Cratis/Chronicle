// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Identifies the observer and tenant receiving a delivery.
/// </summary>
/// <param name="ObserverType">The kind of observer.</param>
/// <param name="ObserverId">The observer identifier.</param>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
internal record DeliveryTarget(ObserverType ObserverType, string ObserverId, EventStoreName EventStore, EventStoreNamespaceName Namespace);
