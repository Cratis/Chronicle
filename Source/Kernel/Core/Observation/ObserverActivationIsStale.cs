// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// The exception that is thrown when an observer activation was formed while its definition was on another event
/// sequence, and a registration has since moved the definition onto the activation's own event sequence.
/// </summary>
/// <param name="observerKey">The key the observer was addressed with.</param>
/// <param name="activatedOnEventSequenceId">The event sequence the definition was on when the activation was formed.</param>
public class ObserverActivationIsStale(ObserverKey observerKey, EventSequenceId activatedOnEventSequenceId)
    : Exception($"Observer '{observerKey.ObserverId}' on event sequence '{observerKey.EventSequenceId}' was activated while it observed event sequence '{activatedOnEventSequenceId}', and has since been registered on '{observerKey.EventSequenceId}'. The activation is being replaced; retry the operation.");
