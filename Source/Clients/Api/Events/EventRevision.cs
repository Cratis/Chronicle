// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.Identities;

namespace Cratis.Chronicle.Api.Events;

/// <summary>
/// Represents a revision applied to an event.
/// </summary>
/// <param name="Generation">The event type generation of this revision.</param>
/// <param name="CorrelationId">The correlation id of the revision.</param>
/// <param name="CausedBy">Who or what caused the revision.</param>
/// <param name="Occurred">When the revision occurred.</param>
/// <param name="Content">The JSON content of the revising event.</param>
public record EventRevision(
    uint Generation,
    string CorrelationId,
    Identity CausedBy,
    DateTimeOffset Occurred,
    string Content);
