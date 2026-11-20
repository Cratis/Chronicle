// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>How many iterations a profile runs and how long it may take.</summary>
/// <param name="WarmupCount">The number of BenchmarkDotNet warmup iterations.</param>
/// <param name="IterationCount">The number of BenchmarkDotNet measured iterations.</param>
/// <param name="TimeoutMinutes">The wall-clock budget of the whole run.</param>
/// <param name="RequiresConfirmation">Whether the run is long enough to need an explicit confirmation flag.</param>
public sealed record RunPlan(int WarmupCount, int IterationCount, int TimeoutMinutes, bool RequiresConfirmation);
