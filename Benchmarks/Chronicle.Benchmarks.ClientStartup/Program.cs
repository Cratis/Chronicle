// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Cratis.Chronicle.Benchmarks.ClientStartup;

var config = ManualConfig.Create(DefaultConfig.Instance)
    .AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance));
var summaries = BenchmarkSwitcher.FromAssembly(typeof(ClientStartupBenchmark).Assembly).Run(args, config).ToArray();
return summaries.Length == 0 || summaries.Any(summary => summary.HasCriticalValidationErrors || summary.Reports.Any(report => !report.Success)) ? 1 : 0;
