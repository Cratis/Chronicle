// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_QueryCapture;

public class when_explaining_a_sqlite_cursor : Specification
{
    SqlProviderHost _host = default!;
    string _plan = default!;
    string _indexes = default!;
    string _report = default!;
    readonly string _directory = Path.Combine(RepositoryRoot(), ".ai-work", "harness-batch", "spec-plans", Guid.NewGuid().ToString("N"));
    static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, ".git")) && !Directory.Exists(Path.Combine(directory.FullName, ".git"))) directory = directory.Parent ?? throw new InvalidOperationException("Repository root not found.");
        return directory.FullName;
    }
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
        _report = await File.ReadAllTextAsync(Path.Combine(_directory, "plans.md"));
    }
    async Task Destroy() => await _host.DisposeAsync();
    [Fact] void should_capture_the_first_page() => _report.ShouldContain("## cursor-page");
    [Fact] void should_capture_the_deep_page() => _report.ShouldContain("## cursor-deep");
    [Fact] void should_capture_the_type_filter() => _report.ShouldContain("## type-filtered");
    [Fact] void should_capture_the_tail() => _report.ShouldContain("## tail\n");
    [Fact] void should_capture_the_tail_per_type() => _report.ShouldContain("## tail-per-type");
    [Fact] void should_explain_a_real_indexed_query() => _plan.ShouldContain("USING INTEGER PRIMARY KEY");
    [Fact] void should_dump_index_definitions() => _indexes.ShouldContain("CREATE");
    [Fact] void should_not_report_a_capture_failure() => _plan.ShouldNotContain("not captured");
}
