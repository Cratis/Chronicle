// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Benchmarks.HotPaths;

/// <summary>
/// Compares a direct <c>ILogger.LogError</c> call with a source-generated <see cref="LoggerMessageAttribute"/> method
/// when the level is disabled. Run with --filter '*LoggerMessageBenchmark*' --runtimes net10.0 net11.0 --memory.
/// </summary>
[MemoryDiagnoser]
public partial class LoggerMessageBenchmark : IDisposable
{
    readonly ILoggerFactory _factory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Critical));
    readonly ILogger _logger;
    readonly Exception _error = new InvalidOperationException("benchmark");

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerMessageBenchmark"/> class.
    /// </summary>
    public LoggerMessageBenchmark() => _logger = _factory.CreateLogger<LoggerMessageBenchmark>();

    /// <summary>
    /// Calls the logger directly with a message template and arguments.
    /// </summary>
    [Benchmark(Baseline = true)]
#pragma warning disable CA1848, CA1873 // The point of the benchmark is the direct call.
    public void Direct() => _logger.LogError(_error, "Error when trying to resolve join");
#pragma warning restore CA1848, CA1873

    /// <summary>
    /// Calls the source-generated log method.
    /// </summary>
    [Benchmark]
    public void Generated() => Failed(_logger, _error);

    /// <inheritdoc/>
    public void Dispose() => _factory.Dispose();

    [LoggerMessage(LogLevel.Error, "Error when trying to resolve join")]
    static partial void Failed(ILogger logger, Exception error);
}
