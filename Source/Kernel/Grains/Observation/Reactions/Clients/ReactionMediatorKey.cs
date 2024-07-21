// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Observation.Reactions;

namespace Cratis.Chronicle.Grains.Observation.Reactions.Clients;

/// <summary>
/// Represents a key used for the <see cref="IReactionMediator"/> to track observer subscriptions.
/// </summary>
/// <param name="ObserverId">Observer the key is for.</param>
/// <param name="ConnectionId">Connection the key is for.</param>
public record ReactionMediatorKey(ReactionId ObserverId, ConnectionId ConnectionId);
