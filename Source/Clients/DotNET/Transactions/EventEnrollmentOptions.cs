// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Options for enrolling one event, including the provenance of its scope.
/// </summary>
/// <param name="EventStreamType">Optional stream type.</param>
/// <param name="EventStreamId">Optional stream identifier.</param>
/// <param name="EventSourceType">Optional source type.</param>
/// <param name="ConcurrencyScope">Optional concurrency scope.</param>
/// <param name="Origin">The contribution supplying the scope.</param>
/// <param name="Tags">Optional plain tags.</param>
/// <param name="NamedTags">Optional structured tags.</param>
/// <param name="Occurred">Optional occurrence time.</param>
/// <param name="Subject">Optional subject.</param>
public record EventEnrollmentOptions(
    EventStreamType? EventStreamType = default,
    EventStreamId? EventStreamId = default,
    EventSourceType? EventSourceType = default,
    ConcurrencyScope? ConcurrencyScope = default,
    ConcurrencyScopeOrigin Origin = ConcurrencyScopeOrigin.Explicit,
    IEnumerable<string>? Tags = default,
    IEnumerable<NamedTag>? NamedTags = default,
    DateTimeOffset? Occurred = default,
    Subject? Subject = default)
{
    /// <summary>
    /// Gets or inits dimensions resolved at commit instead of a precomputed routing scope.
    /// </summary>
    public ConcurrencyDimensions? DeclaredConcurrency { get; init; }
}
