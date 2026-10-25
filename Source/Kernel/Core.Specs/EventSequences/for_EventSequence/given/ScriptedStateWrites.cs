// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// Names which of the grain state writes (counted from one) fail.
/// </summary>
public class ScriptedStateWrites
{
    /// <summary>
    /// Gets the one-based write numbers that fail.
    /// </summary>
    public HashSet<int> FailingWrites { get; } = [];

    /// <summary>
    /// Gets the number of writes attempted so far.
    /// </summary>
    public int Attempted { get; private set; }

    /// <summary>
    /// Counts a write attempt.
    /// </summary>
    /// <returns>True when the write succeeds; false when it is scripted to fail.</returns>
    public bool Next() => !FailingWrites.Contains(++Attempted);
}
