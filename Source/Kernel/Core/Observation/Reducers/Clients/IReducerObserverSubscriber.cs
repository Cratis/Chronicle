// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Defines a client reducer subscriber that will receive all events.
/// </summary>
public interface IReducerObserverSubscriber : IObserverSubscriber, IAmOwnedByClient;
