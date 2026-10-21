// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes;

public class and_the_provider_is_sqlite : given.an_indexed_sink
{
    readonly CapturingLogger _logger = new();

    protected override ISqlSinkHarness CreateHarness() => new SqlSinkHarness { Logger = _logger };

    Task Because() => EnsureAndReplay();

    [Fact] void should_create_only_supported_scalar_indexes() => _indexedColumns.ShouldEqual(["name", "number]quote"]);
    [Fact] void should_create_indexes_on_the_existing_table() => _initialCount.ShouldEqual(2);
    [Fact] void should_be_idempotent() => _repeatedCount.ShouldEqual(2);
    [Fact] void should_index_the_shadow() => _shadowCount.ShouldEqual(2);
    [Fact] void should_preserve_indexes_on_promotion() => _promotedCount.ShouldEqual(2);
    [Fact] void should_allow_a_second_indexed_shadow() => _secondShadowCount.ShouldEqual(2);
    [Fact] void should_preserve_indexes_on_the_second_promotion() => _secondPromotedCount.ShouldEqual(2);

    [Fact] void should_warn_about_each_unsupported_path_of_the_read_model() => string.Join('\n', Warnings()).ShouldEqual(string.Join('\n', new[] { "lines", "lines.value", "missing", "nested", "nested.value" }.Select(path => $"SQL index for indexed-models at {path} was not created: only top-level scalar columns are supported; nested paths, objects and arrays require SQL expression indexes")));

    IEnumerable<string> Warnings() => _logger.Warnings.Distinct().Order(StringComparer.Ordinal);

    sealed class CapturingLogger : ILogger<Sink>
    {
        readonly ConcurrentQueue<string> _warnings = new();

        public IEnumerable<string> Warnings => _warnings;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                _warnings.Enqueue(formatter(state, exception));
            }
        }
    }
}
