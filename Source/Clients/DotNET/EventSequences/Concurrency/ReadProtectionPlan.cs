// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// The resolved scopes and their diagnostic descriptions for one commit.
/// </summary>
/// <param name="Scopes">The conditions to send.</param>
/// <param name="Conditions">The descriptions exposed by the unit of work.</param>
internal record ReadProtectionPlan(IReadOnlyDictionary<EventSourceId, ConcurrencyScope> Scopes, IReadOnlyList<AppendCondition> Conditions);
