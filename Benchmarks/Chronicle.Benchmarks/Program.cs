// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;
using Cratis.Chronicle.Benchmarks;

var benchmarkArguments = HasFilter(args) ? args : ["--filter", "*", .. args];
var summaries = BenchmarkSwitcher.FromAssembly(typeof(AppendBenchmark).Assembly).Run(benchmarkArguments);

return BenchmarkRunOutcome.ExitCodeFor(summaries, Console.Error);

static bool HasFilter(string[] arguments) =>
    arguments.Any(argument =>
        argument.Equals("--filter", StringComparison.Ordinal) ||
        argument.StartsWith("--filter=", StringComparison.Ordinal));
