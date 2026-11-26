// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Attributes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_CountedPass;

[Collection(nameof(StorageRunOptionsCollection))]
public class when_the_budget_runs_out_during_a_counted_pass : Specification
{
    bool _cutShort;
    long _completed;
    async Task Because()
    {
        var previous = RunBudget.Current;
        var clock = new for_RunBudget.when_the_budget_has_time_left.Clock();
        using var budget = new RunBudget(TimeSpan.FromMinutes(1), clock);
        try
        {
            RunBudget.Current = budget;
            Measurements.Clear();
            BudgetCase.Complete = () => clock.Elapsed = TimeSpan.FromMinutes(1);
            BudgetCase.Cleaned = false;
            _cutShort = await CountedPass.Run([typeof(BudgetCase)], 3, "*");
            _completed = Measurements.For("completed").Operations;
        }
        finally
        {
            RunBudget.Current = previous;
            Measurements.Clear();
        }
    }
    [Fact] void should_report_the_cut() => _cutShort.ShouldBeTrue();
    [Fact] void should_keep_only_the_completed_iteration() => _completed.ShouldEqual(1L);
    [Fact] void should_run_cleanup() => BudgetCase.Cleaned.ShouldBeTrue();

    public class BudgetCase
    {
        internal static Action Complete = () => { };
        internal static bool Cleaned;
        [Benchmark]
        public void Operation()
        {
            Measurements.Record("completed", new(1, 1, new(0, new Dictionary<string, long>()), [TimeSpan.FromMilliseconds(1)], 0));
            Complete();
        }
        [GlobalCleanup] public void Cleanup() => Cleaned = true;
    }
}
