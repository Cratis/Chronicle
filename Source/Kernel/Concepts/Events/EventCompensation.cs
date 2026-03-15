// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the metadata of a compensation applied to an event.
/// </summary>
/// <param name="EventTypeGeneration">The <see cref="EventTypeGeneration"/> of the compensating content.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> of the compensation.</param>
/// <param name="Causation">The chain of <see cref="Causation"/> for the compensation.</param>
/// <param name="CausedBy">The <see cref="Identity"/> that caused the compensation.</param>
/// <param name="Occurred">When the compensation occurred.</param>
/// <param name="Content">The compensating content as a JSON string.</param>
public record EventCompensation(
    EventTypeGeneration EventTypeGeneration,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    Identity CausedBy,
    DateTimeOffset Occurred,
    string Content = "");
