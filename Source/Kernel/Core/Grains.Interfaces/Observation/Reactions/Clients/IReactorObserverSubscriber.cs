// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Observation.Reactors.Clients;

/// <summary>
/// Defines a client observer subscriber that will receive all events it subscribes to.
/// </summary>
public interface IReactorObserverSubscriber : IObserverSubscriber, IAmOwnedByClient;
