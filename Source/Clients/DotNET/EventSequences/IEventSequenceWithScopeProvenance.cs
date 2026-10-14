// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The client-side append path that applies implicit guards after routing resolution.
/// </summary>
internal interface IEventSequenceWithScopeProvenance
{
    /// <summary>
    /// Appends a batch with the captured provenance.
    /// </summary>
    /// <param name="events">The events.</param>
    /// <param name="namedTags">Call-level structured tags.</param>
    /// <param name="scopes">Existing concurrency scopes.</param>
    /// <param name="provenance">Scope contributions for this commit.</param>
    /// <returns>The append result.</returns>
    Task<AppendManyResult> AppendMany(
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<NamedTag> namedTags,
        IDictionary<EventSourceId, ConcurrencyScope> scopes,
        ScopeProvenance provenance);

    /// <summary>
    /// Appends only through the unit's internal Enforce authority.
    /// </summary>
    /// <param name="events">The captured events.</param>
    /// <param name="namedTags">Call-level structured tags.</param>
    /// <param name="scopes">Legacy resolution for labels without reads.</param>
    /// <param name="commit">The immutable acquisitions and target.</param>
    /// <returns>The append result.</returns>
    /// <exception cref="EventSequenceReadProtectionNotSupported">The implementation has no trusted path.</exception>
    Task<AppendManyResult> AppendMany(
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<NamedTag> namedTags,
        IDictionary<EventSourceId, ConcurrencyScope> scopes,
        ReadProtectionCommit commit) => throw new EventSequenceReadProtectionNotSupported(GetType());
}
