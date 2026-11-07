// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;
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
        var summaries = BenchmarkSwitcher.FromAssembly(typeof(AppendBenchmark).Assembly).Run(benchmarkArguments);

        return BenchmarkRunOutcome.ExitCodeFor(summaries, Console.Error);
    }

    static bool HasFilter(string[] arguments) =>
        arguments.Any(argument =>
            argument.Equals("--filter", StringComparison.Ordinal) ||
            argument.StartsWith("--filter=", StringComparison.Ordinal));
}
