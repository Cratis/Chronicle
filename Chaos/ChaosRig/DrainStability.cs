// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Requires three finding-free samples with unchanged durable progress, ignoring redelivery counts.
/// </summary>
internal sealed class DrainStability
{
    VerdictInput? _previous;
    int _samples;

    /// <summary>
    /// Observes the durable tail, observer positions and projection/reducer state.
    /// </summary>
    /// <param name="input">A complete collected sample.</param>
    /// <returns>Whether the stable, finding-free window has been observed.</returns>
    public bool Observe(VerdictInput input)
    {
        if (Verdict.Compare(input).HasFindings)
        {
            Reset();
            return false;
        }
        _samples = _previous is not null && SameProgress(_previous, input) ? _samples + 1 : 1;
        _previous = input;
        return _samples >= 3;
    }

    /// <summary>
    /// Breaks the consecutive-sample window after a collection failure or an actual finding.
    /// </summary>
    public void Reset()
    {
        _previous = null;
        _samples = 0;
    }

    static bool SameProgress(VerdictInput previous, VerdictInput current) =>
        previous.Events.Max(item => item.Sequence) == current.Events.Max(item => item.Sequence) &&
        previous.Observers.OrderBy(item => item.Id, StringComparer.Ordinal).SequenceEqual(current.Observers.OrderBy(item => item.Id, StringComparer.Ordinal)) &&
        previous.Projections.OrderBy(item => item.Source, StringComparer.Ordinal).SequenceEqual(current.Projections.OrderBy(item => item.Source, StringComparer.Ordinal)) &&
        previous.Reductions.OrderBy(item => item.Source, StringComparer.Ordinal).SequenceEqual(current.Reductions.OrderBy(item => item.Source, StringComparer.Ordinal));
}
