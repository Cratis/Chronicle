// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// A snapshot of scope contributions for one commit.
/// </summary>
/// <param name="Origins">Accumulated concrete enrollment origins.</param>
/// <param name="DecisionLabels">Labels contributed by decision reads.</param>
/// <param name="ImplicitScopes">Implicit boundaries for the bound sequence.</param>
internal record ScopeProvenance(
    IReadOnlyDictionary<EventSourceId, ScopeOrigins> Origins,
    IReadOnlySet<EventSourceId> DecisionLabels,
    IReadOnlyDictionary<EventSourceId, EventSequenceNumber> ImplicitScopes);
