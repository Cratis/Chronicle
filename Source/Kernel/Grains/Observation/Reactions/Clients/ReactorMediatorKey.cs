// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Clients;

/// <summary>
/// Represents a key used for the <see cref="IReactorMediator"/> to track observer subscriptions.
/// </summary>
/// <param name="ReactorId">The Reactor the key is for.</param>
/// <param name="ConnectionId">Connection the key is for.</param>
public record ReactorMediatorKey(ReactorId ReactorId, ConnectionId ConnectionId);
