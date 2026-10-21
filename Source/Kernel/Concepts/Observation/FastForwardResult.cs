// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the result of fast forwarding an observer.
/// </summary>
/// <param name="Outcome">The <see cref="FastForwardOutcome"/> of the operation.</param>
/// <param name="PreviousNextEventSequenceNumber">The next <see cref="EventSequenceNumber"/> the observer expected before the fast forward.</param>
/// <param name="NextEventSequenceNumber">The next <see cref="EventSequenceNumber"/> the observer expects after the fast forward.</param>
[GenerateSerializer]
[Alias(nameof(FastForwardResult))]
public record FastForwardResult(
    FastForwardOutcome Outcome,
    EventSequenceNumber PreviousNextEventSequenceNumber,
    EventSequenceNumber NextEventSequenceNumber);
