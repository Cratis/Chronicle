// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>The completed append and observation measurement.</summary>
/// <param name="Appended">The measured event count, excluding seed events.</param>
/// <param name="Observed">The distinct observed count, including seed events.</param>
/// <param name="AppendSeconds">Append-only wall time.</param>
/// <param name="ObserverCatchUpMs">First-append-to-observation wall time.</param>
internal sealed record WorkloadResult(int Appended, int Observed, double AppendSeconds, double ObserverCatchUpMs)
{
    /// <summary>Gets measured append throughput.</summary>
    public double AppendEventsPerSecond => Appended / AppendSeconds;
}
