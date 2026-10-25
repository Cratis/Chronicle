// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Represents the refusal of an observer definition because a later generation owns its identity.
/// </summary>
/// <param name="store">The event store.</param>
/// <param name="observerId">The observer identity.</param>
public sealed class ObserverIdentityClaimedByGeneration(EventStoreName store, ObserverId observerId)
    : Exception($"The observer identity '{observerId}' in event store '{store}' is owned by a materialization generation and cannot be registered as an observer definition.");
