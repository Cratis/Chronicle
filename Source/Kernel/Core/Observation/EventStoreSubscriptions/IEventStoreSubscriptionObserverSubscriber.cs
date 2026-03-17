// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Defines a specialized <see cref="IObserverSubscriber"/> for event store subscriptions.
/// </summary>
public interface IEventStoreSubscriptionObserverSubscriber : IObserverSubscriber, IAmOwnedByKernel;
