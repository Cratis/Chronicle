// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Settings for the catch-up shape scenario.
/// </summary>
/// <param name="Sources">The number of event sources.</param>
/// <param name="Events">The number of events.</param>
/// <param name="BatchSize">The append batch size.</param>
/// <param name="TimeoutSeconds">The timeout in seconds.</param>
/// <param name="ObserverType">The observer type, projection or reactor.</param>
internal sealed record CatchUpShapeSettings(int Sources, int Events, int BatchSize, int TimeoutSeconds, string ObserverType)
{
    /// <summary>
    /// Gets the maximum post-timing patterns settlement wait in seconds.
    /// </summary>
    public int PatternsWaitSeconds { get; init; } = 900;

    /// <summary>
    /// Gets the delay between live-interleave append batches in milliseconds.
    /// </summary>
    public int AppendIntervalMs { get; init; }

    /// <summary>
    /// Reads the settings from environment variables.
    /// </summary>
    /// <returns>The settings.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CATCHUP_OBSERVER is not projection or reactor.</exception>
    public static CatchUpShapeSettings FromEnvironment()
    {
        var observerType = Environment.GetEnvironmentVariable("CATCHUP_OBSERVER") ?? "projection";
        if (observerType is not ("projection" or "reactor"))
        {
            throw new InvalidOperationException("CATCHUP_OBSERVER must be projection or reactor.");
        }

        return new(
            ReadPositive("CATCHUP_SOURCES", 100),
            ReadPositive("CATCHUP_EVENTS", 10000),
            ReadPositive("CATCHUP_BATCH_SIZE", 100),
            ReadPositive("CATCHUP_TIMEOUT_SECONDS", 180),
            observerType)
        {
            PatternsWaitSeconds = ReadPositive("CATCHUP_PATTERNS_WAIT_SECONDS", 900)
        };
    }

    /// <summary>
    /// Reads the live-interleave settings from environment variables.
    /// </summary>
    /// <returns>The settings, including the append interval.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the append interval is not a non-negative integer.</exception>
    public static CatchUpShapeSettings FromLiveEnvironment()
    {
        var settings = FromEnvironment();
        var text = Environment.GetEnvironmentVariable("CATCHUP_APPEND_INTERVAL_MS");
        if (text is null) return settings;
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var interval) && interval >= 0)
        {
            return settings with { AppendIntervalMs = interval };
        }

        throw new InvalidOperationException("CATCHUP_APPEND_INTERVAL_MS must be a non-negative integer.");
    }

    static int ReadPositive(string name, int fallback)
    {
        var text = Environment.GetEnvironmentVariable(name);
        if (text is null) return fallback;
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0) return value;
        throw new InvalidOperationException($"{name} must be a positive integer.");
    }
}
