// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_QueryCapture;

[Collection(nameof(StorageRunOptionsCollection))]
public class when_explaining_a_sqlite_cursor : Specification
{
    SqlProviderHost _host = default!;
    string _plan = default!;
    string _indexes = default!;
    string _report = default!;
    readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    async Task Establish()
    {
        _host = new(StorageProvider.Sqlite);
        await _host.Start();
        Directory.CreateDirectory(_directory);
    }
    async Task Because()
    {
        var storage = _host.CreateSequence();
        await StorageBenchmark.Seed(storage, 10);
        using (_host.Capture.Begin("cursor-page"))
        using (var cursor = await storage.GetFromSequenceNumber(5)) await cursor.MoveNext();
        var query = _host.Capture.Queries["cursor-page"];
        _plan = await _host.Explain(query);
        _indexes = await _host.IndexDefinitions(query);
        await PlanCapture.Run(_host, 10, _directory);
        await PlanCapture.Run(_host, 20, _directory);
        await DrainCapture.Run(_host, 10, _directory);
        await DrainCapture.Run(_host, 20, _directory);
        _report = await File.ReadAllTextAsync(Path.Combine(_directory, "plans.md"));
    }
    async Task Destroy()
    {
        try
        {
            await _host.DisposeAsync();
        }
        finally
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
    }
    [Fact] void should_capture_the_first_page() => _report.ShouldContain("## cursor-page");
    [Fact] void should_capture_the_deep_page() => _report.ShouldContain("## cursor-deep");
    [Fact] void should_capture_the_type_filter() => _report.ShouldContain("## type-filtered");
    [Fact] void should_capture_the_tail() => _report.ShouldContain("## tail\n");
    [Fact] void should_capture_the_tail_per_type() => _report.ShouldContain("## tail-per-type");
    [Fact] void should_explain_a_real_indexed_query() => _plan.ShouldContain("USING INTEGER PRIMARY KEY");
    [Fact] void should_dump_index_definitions() => _indexes.ShouldContain("CREATE");
    [Fact] void should_not_report_a_capture_failure() => _plan.ShouldNotContain("not captured");
    [Fact] void should_keep_the_first_fixture_size() => _report.ShouldContain("10 events");
    [Fact] void should_keep_the_other_fixture_size() => _report.ShouldContain("20 events");
    [Fact] void should_write_page_attribution() => File.ReadAllText(Path.Combine(_directory, "drain-pages.md")).ShouldContain("events read: 10");
    [Fact] void should_write_the_other_drain_size() => File.ReadAllText(Path.Combine(_directory, "drain-pages.md")).ShouldContain("events read: 20");
    [Fact] void should_write_one_drain_heading() => File.ReadLines(Path.Combine(_directory, "drain-pages.md")).Count(line => string.Equals(line, "# Drain page attribution", StringComparison.Ordinal)).ShouldEqual(1);
}
