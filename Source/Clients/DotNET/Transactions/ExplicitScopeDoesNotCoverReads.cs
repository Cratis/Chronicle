// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when an exact condition is narrower or later than protected reads.
/// </summary>
/// <param name="label">The condition label.</param>
/// <param name="reads">The reads the condition fails to protect.</param>
/// <param name="capturedAfter">Whether the boundary was captured too late.</param>
public class ExplicitScopeDoesNotCoverReads(EventSourceId label, IEnumerable<string> reads, bool capturedAfter)
    : Exception($"The command returns an exact concurrency scope for '{label}' that {(capturedAfter ? "was captured after" : "does not watch")} what it read: {string.Join(", ", reads)}. Widen the scope and capture its boundary before reading.");
