// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// One revision's content, hash, and audit metadata persisted in a row's history.
/// </summary>
/// <param name="Generation">The revised generation.</param>
/// <param name="CorrelationId">The revision's correlation.</param>
/// <param name="Causation">The revision's causation chain.</param>
/// <param name="CausedBy">The identity keys of the revision author.</param>
/// <param name="Occurred">When the revision occurred.</param>
/// <param name="Content">The serialized revised content.</param>
/// <param name="Hash">The hash of the revised content.</param>
internal sealed record EventRevisionEntry(
    uint Generation,
    Guid CorrelationId,
    IEnumerable<Causation> Causation,
    IEnumerable<Guid> CausedBy,
    DateTimeOffset Occurred,
    string Content,
    string? Hash = null);
