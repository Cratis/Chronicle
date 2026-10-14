// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Low-cardinality diagnostics for implicit scope displacement.
/// </summary>
internal static class ScopeGuardDiagnostics
{
    static readonly Meter _meter = new("Cratis.Chronicle.KeyGuard");
    static readonly Counter<long> _displaced = _meter.CreateCounter<long>("keyguard.displaced");
    static readonly Counter<long> _otherSequence = _meter.CreateCounter<long>("keyguard.other_sequence");

    /// <summary>
    /// Counts a guard displaced by a caller-selected scope.
    /// </summary>
    internal static void DisplacedByExplicit() => _displaced.Add(1, new KeyValuePair<string, object?>("reason", "explicit"));

    /// <summary>
    /// Counts guards belonging to a sequence other than the bound sequence.
    /// </summary>
    /// <param name="count">The number of discarded contributions.</param>
    internal static void OtherSequence(int count) => _otherSequence.Add(count);
}
