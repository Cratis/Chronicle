// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using BenchmarkDotNet.Running;
using Cratis.Chronicle.Benchmarks;
using Cratis.Chronicle.Benchmarks.ColdStart;

var mainStart = Stopwatch.GetTimestamp();

if (args.FirstOrDefault()?.StartsWith("diagnostic-", StringComparison.Ordinal) == true)
{
    return await StartupDiagnosticRunner.Run(args);
}

if (args is ["probe", var probeScenario])
{
    return await ProbeScenario.Run(probeScenario, mainStart);
}

if (args is ["probe-loop", var loopScenario, var loopCount])
{
    return await ProbeLoop.Run(loopScenario, int.Parse(loopCount));
}

var summaries = BenchmarkSwitcher.FromAssembly(typeof(GrpcServiceProxyCreationBenchmark).Assembly).Run(args);
return BenchmarkRunOutcome.ExitCodeFor(summaries, Console.Error);
