// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents the events about to be enrolled for one event source in one call.
/// </summary>
/// <param name="EventSequenceId">The event sequence receiving the events.</param>
/// <param name="EventSourceId">The event source receiving the events.</param>
/// <param name="SuppliedScope">The caller's scope, or <see cref="ConcurrencyScope.NotSet"/>.</param>
/// <param name="Events">The events for this source in this call.</param>
public record UnitOfWorkEnrollment(
    EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    ConcurrencyScope SuppliedScope,
    IReadOnlyList<object> Events);
