// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;
using Cratis.Chronicle.Benchmarks.ContainerProfile;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>The benchmark entry point, with an opt-in container profiling command.</summary>
internal static class Program
{
    /// <summary>Runs the profiling command or the existing BenchmarkDotNet path.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The selected runner's exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--container-profile")
        {
            return await ProfileCli.Run(args[1..]);
        }
        var benchmarkArguments = HasFilter(args) ? args : ["--filter", "*", .. args];
        var summaries = BenchmarkSwitcher.FromAssembly(typeof(AppendBenchmark).Assembly).Run(benchmarkArguments);
        return BenchmarkRunOutcome.ExitCodeFor(summaries, Console.Error);
    }

    static bool HasFilter(string[] arguments) =>
        arguments.Any(argument =>
            argument.Equals("--filter", StringComparison.Ordinal) ||
            argument.StartsWith("--filter=", StringComparison.Ordinal));
}
