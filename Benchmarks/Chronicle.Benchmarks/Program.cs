// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;

var benchmarkArguments = HasFilter(args) ? args : ["--filter", "*", .. args];
BenchmarkSwitcher.FromAssembly(typeof(Cratis.Chronicle.Benchmarks.AppendBenchmark).Assembly).Run(benchmarkArguments);

static bool HasFilter(string[] arguments) =>
    arguments.Any(argument =>
        argument.Equals("--filter", StringComparison.Ordinal) ||
        argument.StartsWith("--filter=", StringComparison.Ordinal));
