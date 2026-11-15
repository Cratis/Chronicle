// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Cratis.Chronicle.Benchmarks.ContainerProfile;

namespace Cratis.Chronicle.Benchmarks;

#pragma warning disable IDE0210 // A namespaced entry point avoids a type-discovery collision with the server's Program.

/// <summary>
/// Runs the selected benchmarks and reports their outcome, with an opt-in container profiling command.
/// </summary>
internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--container-profile")
        {
            return await ProfileCli.Run(args[1..]);
        }

        var benchmarkArguments = HasFilter(args) ? args : ["--filter", "*", .. args];
        var config = Configuration();
        var summaries = BenchmarkSwitcher.FromAssembly(typeof(AppendBenchmark).Assembly).Run(benchmarkArguments, config);

        return BenchmarkRunOutcome.ExitCodeFor(summaries, Console.Error);
    }

    static ManualConfig? Configuration()
    {
        if (Environment.GetEnvironmentVariable("CRATIS_BENCHMARK_INPROCESS") == "1")
        {
            return ManualConfig.Create(DefaultConfig.Instance).AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance).WithWarmupCount(3).WithIterationCount(15).AsMutator());
        }
        if (Environment.GetEnvironmentVariable("CRATIS_BENCHMARK_SERIAL_BUILD") == "1")
        {
            return ManualConfig.Create(DefaultConfig.Instance).AddJob(Job.Default.WithMsBuildArguments("-m:1", "-p:BuildInParallel=false").AsMutator());
        }
        return null;
    }

    static bool HasFilter(string[] arguments) =>
        arguments.Any(argument =>
            argument.Equals("--filter", StringComparison.Ordinal) ||
            argument.StartsWith("--filter=", StringComparison.Ordinal));
}
