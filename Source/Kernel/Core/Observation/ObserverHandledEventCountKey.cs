// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a cache key that uniquely identifies a handled event count for an observer on a specific event sequence.
/// </summary>
/// <param name="ObserverId">The <see cref="ObserverId"/> of the observer.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the observer is observing.</param>
public record ObserverHandledEventCountKey(ObserverId ObserverId, EventSequenceId EventSequenceId);
