// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Represents a key used for the <see cref="IReducerMediator"/> to track observer subscriptions.
/// </summary>
/// <param name="ReducerId">The Reducer the key is for.</param>
/// <param name="ConnectionId">Connection the key is for.</param>
/// <param name="EventStore">The <see cref="EventStoreName"/> the key is for.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> the key is for.</param>
public record ReducerMediatorKey(ReducerId ReducerId, ConnectionId ConnectionId, EventStoreName EventStore, EventStoreNamespaceName Namespace);
