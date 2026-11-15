// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// The exception that is thrown when an event store subscription could not be refreshed in a namespace.
/// </summary>
/// <param name="subscriptionId">The subscription that could not be refreshed.</param>
/// <param name="namespace">The namespace it could not be refreshed in.</param>
/// <param name="innerException">The failure that stopped it.</param>
public class EventStoreSubscriptionRefreshFailed(EventStoreSubscriptionId subscriptionId, EventStoreNamespaceName @namespace, Exception innerException)
    : Exception($"Event store subscription '{subscriptionId}' could not be refreshed in namespace '{@namespace}'", innerException);
