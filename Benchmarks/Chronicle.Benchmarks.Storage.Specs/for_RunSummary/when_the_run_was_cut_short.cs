// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_RunSummary;

[Collection(nameof(StorageRunOptionsCollection))]
public class when_the_run_was_cut_short : Specification
{
    string _cut = default!;
    string _complete = default!;
    void Because()
    {
        _cut = RunSummary.Combine([], "nonexistent-results", true);
        _complete = RunSummary.Combine([], "nonexistent-results");
    }
    [Fact] void should_explain_the_partial_results() => _cut.ShouldContain("**Run cut short:**");
    [Fact] void should_include_the_profile_budget() => _cut.ShouldContain($"{StorageRunOptions.Current.Profile.TimeoutMinutes} minutes ran out");
    [Fact] void should_not_change_a_complete_summary() => _complete.ShouldNotContain("Run cut short");
}
