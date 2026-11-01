// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;
using Cratis.Chronicle.Benchmarks.Serialization;

var summaries = BenchmarkSwitcher.FromAssembly(typeof(AppendedEventCodecBenchmarks).Assembly).Run(args).ToArray();
return summaries.Length == 0 || summaries.Any(summary => summary.HasCriticalValidationErrors || summary.Reports.Any(report => !report.Success)) ? 1 : 0;
