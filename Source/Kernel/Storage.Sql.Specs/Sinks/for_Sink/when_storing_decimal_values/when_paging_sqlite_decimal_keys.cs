// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_storing_decimal_values;

public class when_paging_sqlite_decimal_keys : Specification
{
    SqlSinkHarness _harness;
    ISink _sink;
    readonly SqlReadCapture _capture = new();
    readonly string[] _values = ["10", "2", "9", "-2", "-10", "0", "0.0000000000000000000000000001", "-0.0000000000000000000000000001", "79228162514264337593543950335", "-79228162514264337593543950335", "193.580", "193.58", "0.1234567890123456789012345678", "0.1234567890123456789012345679"];
    string[] _expected;
    readonly List<string> _paged = [];
    string[] _observed;
    long _total;
    bool _hidden;
    bool _orderedBeforeLimit;
    int _orderedQueries;

    void Establish()
    {
        _harness = new SqlSinkHarness { Interceptors = [_capture] };
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"number","format":"decimal"},"label":{"type":"string"}}}""");
        _sink = _harness.CreateSink(new ReadModelDefinition("decimal-keys", "decimal_keys", "Decimal keys", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "decimal-keys", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        _expected = _values.OrderBy(value => decimal.Parse(value, CultureInfo.InvariantCulture)).ThenBy(value => value, StringComparer.Ordinal).ToArray();
    }

    async Task Because()
    {
        foreach (var value in _values)
        {
            var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
            changeset.InitialState.Returns(new ExpandoObject());
            changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("label", null, value)])]);
            (await _sink.ApplyChanges(new Key(decimal.Parse(value, CultureInfo.InvariantCulture), ArrayIndexers.NoIndexers), changeset, 1UL)).ShouldBeEmpty();
        }
        await using var context = new DbContext(new DbContextOptionsBuilder().UseSqlite(_harness.ConnectionString).Options);

        // An old kernel can promote a table with the original TEXT schema after the cache is warm.
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE old_decimal_keys AS SELECT * FROM decimal_keys; DROP TABLE decimal_keys; CREATE TABLE decimal_keys(id TEXT NOT NULL PRIMARY KEY, label TEXT NULL, __lastHandledEventSequenceNumber INTEGER NULL, __initialized INTEGER NOT NULL DEFAULT 1); INSERT INTO decimal_keys SELECT id,label,__lastHandledEventSequenceNumber,__initialized FROM old_decimal_keys; DROP TABLE old_decimal_keys");
        _capture.Clear();
        for (var skip = 0; skip < _values.Length; skip += 3)
        {
            var page = await _sink.GetInstances(skip: skip, take: 3);
            _total = page.TotalCount;
            _paged.AddRange(page.Instances.Select(entry => (string)((IDictionary<string, object?>)entry)["label"]!));
            _hidden |= page.Instances.Any(entry => ((IDictionary<string, object?>)entry).Keys.Any(key => key.StartsWith("__order_", StringComparison.Ordinal)));
        }
        _orderedQueries = _capture.Commands.Count(command => command.Contains("ORDER BY", StringComparison.Ordinal));
        var sql = _capture.Commands.First(command => command.Contains("ORDER BY", StringComparison.Ordinal));
        _orderedBeforeLimit = sql.Contains("ORDER BY", StringComparison.Ordinal) && sql.Contains("__order_id", StringComparison.Ordinal) && sql.IndexOf("__order_id", sql.IndexOf("ORDER BY", StringComparison.Ordinal), StringComparison.Ordinal) < sql.IndexOf("LIMIT", StringComparison.Ordinal);
        var observed = await _sink.ObserveInstances(skip: 3, take: 3).Take(1).ToTask().WaitAsync(TimeSpan.FromSeconds(10));
        _observed = observed.Select(entry => (string)((IDictionary<string, object?>)entry)["label"]!).ToArray();
    }

    void Destroy() => _harness.Dispose();

    [Fact] void should_page_in_numeric_order_with_a_stable_text_tiebreaker() => _paged.ShouldEqual(_expected);
    [Fact] void should_report_the_total_count() => _total.ShouldEqual(_values.Length);
    [Fact] void should_order_in_sql_before_limiting() => _orderedBeforeLimit.ShouldBeTrue();
    [Fact] void should_observe_the_same_numeric_page() => _observed.ShouldEqual(_expected.Skip(3).Take(3).ToArray());
    [Fact] void should_self_heal_the_missing_ordering_column_and_retry_once() => _orderedQueries.ShouldEqual(((_values.Length + 2) / 3) + 1);
    [Fact] void should_never_expose_internal_columns() => _hidden.ShouldBeFalse();
}
