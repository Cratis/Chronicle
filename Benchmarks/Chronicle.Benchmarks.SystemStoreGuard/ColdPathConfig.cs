// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Cratis.Chronicle.Benchmarks.SystemStoreGuard;

/// <summary>Runs a short batch with one prepared request per invocation.</summary>
public sealed class ColdPathConfig : ManualConfig
{
    /// <summary>Initializes a new instance of the <see cref="ColdPathConfig"/> class.</summary>
    public ColdPathConfig() => AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance).WithInvocationCount(EntitledCaller.RequestCount).WithUnrollFactor(1).WithWarmupCount(10));
}
