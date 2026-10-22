// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Benchmarks.Clustering;

internal sealed record CatchUpShapeSettings(int Sources, int Events, int BatchSize, int TimeoutSeconds, string ObserverType)
{
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
            observerType);
    }

    static int ReadPositive(string name, int fallback)
    {
        var text = Environment.GetEnvironmentVariable(name);
        if (text is null) return fallback;
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0) return value;
        throw new InvalidOperationException($"{name} must be a positive integer.");
    }
}
