// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// The retained catch-up handover, replay outcome or recovery result. Failure includes cursor, hold and owner.
/// </summary>
/// <param name="Outcome">The terminal outcome.</param>
/// <param name="LastHandled">The last handled position, or Unavailable.</param>
/// <param name="LastScanned">The last scanned position, or Unavailable.</param>
/// <param name="AllStepsSucceeded">Whether every step succeeded.</param>
/// <param name="Failure">The failed or held partition when applicable.</param>
public record ObserverJobOutcome(ObserverWorkOutcome Outcome, EventSequenceNumber LastHandled, EventSequenceNumber LastScanned, bool AllStepsSucceeded, FailedPartition? Failure = null);
