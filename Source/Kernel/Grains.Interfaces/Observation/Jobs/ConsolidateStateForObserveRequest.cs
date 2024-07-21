// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactions;

namespace Cratis.Chronicle.Grains.Observation.Jobs;

/// <summary>
/// Represents the request for a <see cref="IReplayObserver"/>.
/// </summary>
/// <param name="Observers">The observers to consolidate state for.</param>
public record ConsolidateStateForObserveRequest(IEnumerable<ObserverIdAndKey> Observers);
