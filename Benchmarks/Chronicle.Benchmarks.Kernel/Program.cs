// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Running;
using Cratis.Chronicle.Benchmarks.Kernel;

var summaries = BenchmarkSwitcher.FromAssembly(typeof(AppendGenerationProtectionBenchmark).Assembly).Run(args).ToArray();
return summaries.Length > 0 && summaries.All(summary => !summary.HasCriticalValidationErrors && summary.Reports.All(report => report.Success)) ? 0 : 1;
