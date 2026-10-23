// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;

if (args.Contains("--rebuild-live", StringComparer.Ordinal))
{
    await Cratis.Chronicle.Benchmarks.Clustering.LiveRebuildScenario.Run();
    return;
}

if (args.Contains("--replay-shape", StringComparer.Ordinal))
{
    await Cratis.Chronicle.Benchmarks.Clustering.ReplayShapeScenario.Run();
    return;
}

var benchmarkArguments = HasFilter(args) ? args : ["--filter", "*", .. args];
BenchmarkSwitcher.FromAssembly(typeof(Cratis.Chronicle.Benchmarks.Clustering.ClusteredAppendBenchmark).Assembly).Run(benchmarkArguments);

static bool HasFilter(string[] arguments) =>
    arguments.Any(argument =>
        argument.Equals("--filter", StringComparison.Ordinal) ||
        argument.StartsWith("--filter=", StringComparison.Ordinal));
