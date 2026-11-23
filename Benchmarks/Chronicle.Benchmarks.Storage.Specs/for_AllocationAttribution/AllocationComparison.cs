// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_AllocationAttribution;

/// <summary>Inclusive paired boundaries, not exclusive allocation stacks or an additive decomposition.</summary>
/// <param name="Boundary">The boundary sampled.</param>
/// <param name="Legacy">Bytes per legacy call.</param>
/// <param name="Clean">Bytes per clean call.</param>
/// <param name="Events">The number of events in a call, or one for a non-event boundary.</param>
internal sealed record AllocationComparison(string Boundary, double Legacy, double Clean, int Events = 1)
{
    /// <summary>Measures a pair after constructing both delegates outside the sampled window.</summary>
    /// <param name="boundary">The boundary name.</param>
    /// <param name="legacy">The baseline operation.</param>
    /// <param name="clean">The cleaned operation.</param>
    /// <param name="events">The events in each call.</param>
    /// <param name="iterations">Warmup and measured calls per arm.</param>
    /// <returns>The paired sample.</returns>
    public static AllocationComparison Measure(string boundary, Action legacy, Action clean, int events = 1, int iterations = 64) =>
        new(boundary, AllocationProbe.Bytes(legacy, iterations), AllocationProbe.Bytes(clean, iterations), events);

    /// <summary>Writes reproducible boundary numbers outside the measured region.</summary>
    /// <param name="output">The specification output.</param>
    public void Write(ITestOutputHelper output) => output.WriteLine(string.Create(
        CultureInfo.InvariantCulture,
        $"ALLOC | {Boundary} | legacy B/call={Legacy:F2} | clean B/call={Clean:F2} | delta B/call={Legacy - Clean:F2} | events/call={Events} | legacy B/event={Legacy / Events:F2} | clean B/event={Clean / Events:F2} | delta B/event={(Legacy - Clean) / Events:F2}"));
}
