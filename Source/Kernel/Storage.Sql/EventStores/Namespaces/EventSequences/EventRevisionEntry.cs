// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents one revision stored in the JSON array held by <see cref="EventEntry.Revisions"/>.
/// </summary>
/// <param name="Generation">The event type generation of the revising content.</param>
/// <param name="CorrelationId">The correlation identifier of the revision.</param>
/// <param name="Causation">The causation chain of the revision.</param>
/// <param name="CausedBy">The identity chain that caused the revision.</param>
/// <param name="Occurred">When the revision occurred.</param>
/// <param name="Content">The revising content as a JSON string.</param>
/// <param name="Hash">The content hash of the revising content.</param>
internal record EventRevisionEntry(
    uint Generation,
    Guid CorrelationId,
    IEnumerable<Causation> Causation,
    IEnumerable<string> CausedBy,
    DateTimeOffset Occurred,
    string Content,
    string Hash);
