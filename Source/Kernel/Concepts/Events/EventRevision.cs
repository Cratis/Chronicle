// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents the metadata of a revision applied to an event.
/// </summary>
/// <param name="EventTypeGeneration">The <see cref="EventTypeGeneration"/> of the revising content.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> of the revision.</param>
/// <param name="Causation">The chain of <see cref="Causation"/> for the revision.</param>
/// <param name="CausedBy">The <see cref="Identity"/> that caused the revision.</param>
/// <param name="Occurred">When the revision occurred.</param>
/// <param name="Content">The revising content as a JSON string.</param>
public record EventRevision(
    EventTypeGeneration EventTypeGeneration,
    CorrelationId CorrelationId,
    IEnumerable<Causation> Causation,
    Identity CausedBy,
    DateTimeOffset Occurred,
    string Content = "");
