// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;
using Cratis.Chronicle.Benchmarks.SqlEventStorage;

var summaries = BenchmarkSwitcher.FromAssembly(typeof(EventSequenceStorageBenchmark).Assembly).Run(args).ToArray();
var failures = summaries.SelectMany(summary => summary.Reports).Count(report => !report.Success || report.ResultStatistics is null);
return summaries.Length > 0 && failures == 0 && summaries.All(summary => !summary.HasCriticalValidationErrors) ? 0 : 1;
