// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The trusted path for carrying a unit's receipt into its one atomic append.
/// </summary>
internal interface IEventSequenceWithEffectReceipts
{
    /// <summary>
    /// Appends events and a receipt in one atomic write, validating any protected reads.
    /// </summary>
    /// <param name="events">The staged events.</param>
    /// <param name="scopes">The required concurrency scopes.</param>
    /// <param name="receipt">The enrolled receipt.</param>
    /// <param name="readProtectionCommit">The trusted read contributions.</param>
    /// <returns>The append result and winning receipt.</returns>
    Task<AppendManyResult> AppendManyWithEffectReceipt(
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> scopes,
        Contracts.Occurrences.EffectReceipt receipt,
        ReadProtectionCommit? readProtectionCommit);
}
