// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;
using Cratis.Chronicle.Benchmarks.Kernel;

if (args.Contains("--observer-footprint"))
{
    return await ObserverFootprint.Run(args);
}

if (args.Contains("--gc-experiment"))
{
    return await GcExperiment.Run(args);
}

if (args.Contains("--gc-workload"))
{
    return await GcWorkload.Run(args);
}

if (args.Contains("--keeper-calls"))
{
    return await KeeperCalls.Run(args);
}

if (args.Contains("--client-streams"))
{
    return await ClientStreams.Run(args);
}

if (args.Contains("--idle-calls"))
{
    return await IdleCalls.Run(args);
}

var summaries = (await BenchmarkSwitcher.FromAssembly(typeof(AppendGenerationProtectionBenchmark).Assembly).RunAsync(args)).ToArray();
return summaries.Length > 0 && summaries.All(summary => !summary.HasCriticalValidationErrors && summary.Reports.All(report => report.Success)) ? 0 : 1;
