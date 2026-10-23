// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// The dimensions and deadline of one replay workload.
/// </summary>
/// <param name="Roots">Roots workload setting.</param>
/// <param name="Depth">Depth workload setting.</param>
/// <param name="Events">Events workload setting.</param>
/// <param name="RunLength">RunLength workload setting.</param>
/// <param name="RemovePercent">RemovePercent workload setting.</param>
/// <param name="TimeoutSeconds">TimeoutSeconds workload setting.</param>
/// <param name="RunLengthMix">Optional distribution overriding the fixed run length.</param>
internal sealed record ReplayShapeSettings(int Roots, int Depth, int Events, int RunLength, int RemovePercent, int TimeoutSeconds, ReplayShapeRunLengthMix? RunLengthMix)
{
    /// <summary>
    /// Reads and validates the environment settings.
    /// </summary>
    /// <returns>The validated workload.</returns>
    /// <exception cref="ArgumentException">A workload setting is invalid or the event budget is insufficient.</exception>
    internal static ReplayShapeSettings Read()
    {
        var mix = ReplayShapeRunLengthMix.Parse(Environment.GetEnvironmentVariable("REPLAY_RUN_LENGTH_MIX"));
        var settings = new ReplayShapeSettings(
            Setting("REPLAY_ROOTS", 2000),
            Setting("REPLAY_DEPTH", 2),
            Setting("REPLAY_EVENTS", 50000),
            mix is null ? Setting("REPLAY_RUN_LENGTH", 1) : 1,
            Setting("REPLAY_REMOVE_PERCENT", 10, 0),
            Setting("REPLAY_TIMEOUT_SECONDS", 1200),
            mix);
        if (settings.Depth > 2) throw new ArgumentException("REPLAY_DEPTH must be 1 or 2; this model-bound shape defines only two child levels.");
        if (settings.RemovePercent > 100) throw new ArgumentException("REPLAY_REMOVE_PERCENT must be between 0 and 100.");
        var minimumEvents = settings.Roots + (2L * settings.Roots * settings.Depth) + (long)Math.Ceiling((long)settings.Roots * settings.RemovePercent / 100d);
        if (settings.Events < minimumEvents) throw new ArgumentException("REPLAY_EVENTS must allow root/child creation, at least one update per child, and leaf removals.");
        return settings;
    }

    static int Setting(string name, int fallback, int minimum = 1)
    {
        if (Environment.GetEnvironmentVariable(name) is not { } value) return fallback;
        return int.TryParse(value, out var parsed) && parsed >= minimum
            ? parsed
            : throw new ArgumentException($"{name} must be an integer >= {minimum}.");
    }
}
