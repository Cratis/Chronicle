// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when an observer is addressed on another event sequence than the one its definition is on.
/// </summary>
/// <param name="observerKey">The key the observer was addressed with.</param>
/// <param name="definitionEventSequenceId">The event sequence the observer's definition is on.</param>
public class ObserverKeyDoesNotMatchDefinition(ObserverKey observerKey, EventSequenceId definitionEventSequenceId)
    : Exception($"Observer '{observerKey.ObserverId}' was addressed on event sequence '{observerKey.EventSequenceId}', but it observes event sequence '{definitionEventSequenceId}'. Address it on '{definitionEventSequenceId}'; registering it on '{observerKey.EventSequenceId}' is what moves it there.");
