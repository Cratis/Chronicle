// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_RunCompletion;

[Collection(nameof(StorageRunOptionsCollection))]
public class when_startup_is_still_pending_after_budget_exhaustion : Specification
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    readonly TaskCompletionSource<IStorageProviderHost> _startup = new();
    int _exitCode;
    string _summary = default!;
    string _counts = default!;
    async Task Because()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "sqlite"));
        Measurements.Clear();
        Measurements.For("sqlite/completed").Apply(new(1, 1, new(1, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(1)], 0));
        var countsPath = Path.Combine(_directory, "sqlite", "counts.json");
        Measurements.Write(countsPath);
        await File.WriteAllTextAsync(Path.Combine(_directory, "sqlite", "summary-part.md"), RunSummary.ForProvider("sqlite"));
        using var budget = new RunBudget(TimeSpan.Zero);
        var completion = new RunCompletion([], _directory, budget.Expired, 0);
        _exitCode = await completion.Finish(() => ProviderHosts.DisposeAll([_startup.Task], TimeSpan.Zero)).WaitAsync(TimeSpan.FromSeconds(1));
        _summary = await File.ReadAllTextAsync(Path.Combine(_directory, "summary.md"));
        _counts = await File.ReadAllTextAsync(countsPath);
    }
    void Destroy()
    {
        Measurements.Clear();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
    [Fact] void should_reach_exit_code_four() => _exitCode.ShouldEqual(4);
    [Fact] void should_not_wait_for_the_stalled_startup() => _startup.Task.IsCompleted.ShouldBeFalse();
    [Fact] void should_publish_the_partial_banner() => _summary.ShouldContain("**Run cut short:**");
    [Fact] void should_keep_the_completed_case() => _summary.ShouldContain("| sqlite | completed | 1 |");
    [Fact] void should_preserve_partial_counts() => _counts.ShouldContain("sqlite/completed");
}
