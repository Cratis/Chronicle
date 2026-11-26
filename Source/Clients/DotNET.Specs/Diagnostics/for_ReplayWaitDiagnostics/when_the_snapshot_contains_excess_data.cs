// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Diagnostics.for_ReplayWaitDiagnostics;

public class when_the_snapshot_contains_excess_data : Specification
{
    const string DetailsSentinel = "SECRET-DETAILS";
    const string ProgressSentinel = "SECRET-PROGRESS";
    const string HostilePrefix = "ab\r\n\u001b\u2028\u2029\u202e";
    const int RetainedCharacters = ReplayWaitDiagnostics.MaxIdentifierLength - 14;
    Exception? _error;

    async Task Because()
    {
        var jobs = Enumerable.Range(0, 25).Select(index => new Job(Substitute.For<IEventStore>())
        {
            Id = Guid.NewGuid(),
            Type = HostilePrefix + new string('x', 300) + "\ntail",
            Details = DetailsSentinel,
            Progress = new() { Message = ProgressSentinel }
        }).ToArray();
        var sources = new ReplayWaitSources(
            () => Task.FromResult<IEnumerable<Job>>(jobs),
            "SomeReactor",
            () => Task.FromResult(new ReactorState(new ReactorId("r"), ObserverRunningState.Active, true, 1UL, 2UL, 3UL)),
            "SomeReducer",
            () => Task.FromResult(new ReducerState(ObserverRunningState.Active, true, 4UL, 5UL, 6UL)),
            "SomeProjection",
            () => Task.FromResult(new ProjectionState(ObserverRunningState.Active, true, 7UL, 8UL, 9UL)));

        try
        {
            await ReplayWaitDiagnostics.Run(() => Task.FromException(new TimeoutException()), sources);
        }
        catch (Exception exception)
        {
            _error = exception;
        }
    }

    string Snapshot => _error!.Message[(_error.Message.IndexOf(Environment.NewLine) + Environment.NewLine.Length)..];

    [Fact] void should_bound_the_snapshot() => (Snapshot.Length <= ReplayWaitDiagnostics.MaxSummaryLength).ShouldBeTrue();
    [Fact] void should_bound_the_jobs_section() => (Snapshot.IndexOf("reactor SomeReactor") - Snapshot.IndexOf("jobs:") <= ReplayWaitDiagnostics.MaxSectionLength + Environment.NewLine.Length).ShouldBeTrue();
    [Fact] void should_replace_line_forging_and_formatting_characters() => Snapshot.Any(_ => _ is '\u001b' or '\u2028' or '\u2029' or '\u202e').ShouldBeFalse();
    [Fact] void should_mark_truncation() => _error!.Message.ShouldContain("[truncated]");
    [Fact] void should_not_include_job_details() => _error!.Message.ShouldNotContain(DetailsSentinel);
    [Fact] void should_not_include_progress_messages() => _error!.Message.ShouldNotContain(ProgressSentinel);
    [Fact] void should_truncate_the_type_within_the_limit_with_the_marker_included() => _error!.Message.ShouldContain("type=ab??????" + new string('x', RetainedCharacters - 8) + "...[truncated] status=");
    [Fact] void should_still_name_the_observers() => _error!.Message.ShouldContain("projection SomeProjection");
}
