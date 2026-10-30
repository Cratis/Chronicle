// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;

if (args.Contains("--fence-cost", StringComparer.Ordinal))
{
    await Cratis.Chronicle.Benchmarks.Clustering.FenceCostScenario.Run();
    return;
}

if (args.Contains("--fence-ddl", StringComparer.Ordinal))
{
    await Cratis.Chronicle.Benchmarks.Clustering.FenceCostDdlScenario.Run();
    return;
}

if (Array.IndexOf(args, "--rebuild") is var rebuildIndex && rebuildIndex >= 0)
{
    try
    {
        if (rebuildIndex + 1 >= args.Length)
        {
            throw new ArgumentException("--rebuild requires S1, S2, S3 or S4.");
        }

        await Cratis.Chronicle.Benchmarks.Clustering.RebuildBaselineScenario.Run(args[rebuildIndex + 1]);
    }
    catch (ArgumentException error)
    {
        await Console.Error.WriteLineAsync(error.Message);
        Environment.ExitCode = 1;
    }

    return;
}

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

if (args.Contains("--catch-up-shape", StringComparer.Ordinal))
{
    await Cratis.Chronicle.Benchmarks.Clustering.CatchUpShapeScenario.Run();
    return;
}

if (args.Contains("--live-interleave", StringComparer.Ordinal))
{
    await Cratis.Chronicle.Benchmarks.Clustering.LiveInterleaveScenario.Run();
    return;
}

var benchmarkArguments = HasFilter(args) ? args : ["--filter", "*", .. args];
BenchmarkSwitcher.FromAssembly(typeof(Cratis.Chronicle.Benchmarks.Clustering.ClusteredAppendBenchmark).Assembly).Run(benchmarkArguments);

static bool HasFilter(string[] arguments) =>
    arguments.Any(argument =>
        argument.Equals("--filter", StringComparison.Ordinal) ||
        argument.StartsWith("--filter=", StringComparison.Ordinal));
