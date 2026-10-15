// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;

if (args.Length > 0 && args[0] == "probe")
{
    Environment.ExitCode = await Cratis.Chronicle.Benchmarks.Probe.Run(args.Skip(1).ToArray());
    return;
}

var benchmarkArguments = HasFilter(args) ? args : ["--filter", "*", .. args];
BenchmarkSwitcher.FromAssembly(typeof(Cratis.Chronicle.Benchmarks.AppendBenchmark).Assembly).Run(benchmarkArguments);

static bool HasFilter(string[] arguments) =>
    arguments.Any(argument =>
        argument.Equals("--filter", StringComparison.Ordinal) ||
        argument.StartsWith("--filter=", StringComparison.Ordinal));
