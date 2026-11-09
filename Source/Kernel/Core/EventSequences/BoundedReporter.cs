// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Allows a fixed number of reports, so input that can vary without limit cannot make a log or any memory grow without limit.
/// </summary>
/// <param name="maximum">The number of reports allowed.</param>
internal sealed class BoundedReporter(int maximum)
{
    int _reported;

    /// <summary>
    /// Take one of the allowed reports.
    /// </summary>
    /// <returns>True when the caller may report; false once the allowance is used. Never allocates and keeps nothing per reported value.</returns>
    public bool TryReport() =>
        Volatile.Read(ref _reported) < maximum && Interlocked.Increment(ref _reported) <= maximum;
}
