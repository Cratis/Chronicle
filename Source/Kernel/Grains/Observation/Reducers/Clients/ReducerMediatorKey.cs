// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Observation.Reducers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents a key used for the <see cref="IReducerMediator"/> to track observer subscriptions.
/// </summary>
/// <param name="ReducerId">The reaction the key is for.</param>
/// <param name="ConnectionId">Connection the key is for.</param>
public record ReducerMediatorKey(ReducerId ReducerId, ConnectionId ConnectionId);
