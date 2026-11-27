// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Diagnostics.for_ReplayWaitDiagnostics;

public class when_the_failure_poll_job_has_already_disappeared : Specification
{
    readonly Job _job = new(Substitute.For<IEventStore>()) { Id = Guid.NewGuid(), Type = "ReplayObserverPartition", Status = JobStatus.CompletedWithFailures };
    readonly JobStepId _step = JobStepId.New();
    JobFailedWhileWaiting _original;
    ReplayWaitDidNotComplete? _error;
    int _stepReads;

    void Establish() => _original = new JobFailedWhileWaiting(_job);

    async Task Because()
    {
        var sources = new ReplayWaitSources(
            () => Task.FromResult<IEnumerable<Job>>([]),
            "ScenarioReactor",
            () => Task.FromResult(new ReactorState("scenario-id", ObserverRunningState.Active, true, 1UL, 2UL, 3UL)),
            "ScenarioReducer",
            () => throw new InvalidOperationException("confidential-reducer-error"),
            "ScenarioProjection",
            () => Task.FromResult(new Projections.ProjectionState(ObserverRunningState.Active, true, 1UL, 2UL, 3UL)),
            GetJobScope: id => Task.FromResult<(string, string)?>(id == _job.Id ? ("actual-owner\n", "actual-partition") : null),
            GetJobSteps: GetSteps,
            ScenarioSource: "scenario-source");

        try
        {
            await ReplayWaitDiagnostics.Run(() => throw _original, sources);
        }
        catch (ReplayWaitDidNotComplete error)
        {
            _error = error;
        }
    }

    Task<IEnumerable<JobStep>> GetSteps(Job job)
    {
        _stepReads++;
        if (job.Id != _job.Id) return Task.FromResult<IEnumerable<JobStep>>([]);

        var terminal = new JobStepStatusChanged
        {
            Occurred = DateTimeOffset.UnixEpoch,
            Status = JobStepStatus.CompletedWithFailure,
            ExceptionMessages = ["confidential-step-error"],
            ExceptionStackTrace = "confidential-stack"
        };
        var later = Enumerable.Range(1, 6).Select(index => new JobStepStatusChanged { Occurred = terminal.Occurred.AddSeconds(index), Status = JobStepStatus.Removing });
        return Task.FromResult<IEnumerable<JobStep>>(
        [
            new() { Status = JobStepStatus.CompletedSuccessfully },
            new() { Id = _step, Name = "confidential-step-name", Status = JobStepStatus.CompletedWithFailure, StatusChanges = later.Append(terminal) }
        ]);
    }

    [Fact] void should_preserve_the_original_failure() => _error!.InnerException.ShouldBeSame(_original);
    [Fact] void should_use_the_failed_job_from_the_poll() => _error!.Message.ShouldContain(_job.Id.Value.ToString("D"));
    [Fact] void should_show_the_failed_step() => _error!.Message.ShouldContain(_step.Value.ToString("D"));
    [Fact] void should_retain_the_terminal_transition_when_history_is_truncated() => _error!.Message.ShouldContain($"{DateTimeOffset.UnixEpoch:O} CompletedWithFailure");
    [Fact] void should_use_the_actual_job_scope_and_sanitize_it() => _error!.Message.ShouldContain("observer=actual-owner? partition=actual-partition");
    [Fact] void should_distinguish_scenario_context_from_job_scope() => _error!.Message.ShouldContain("scenario context: source=scenario-source");
    [Fact] void should_omit_exception_payloads_and_step_names() => _error!.Message.ShouldNotContain("confidential-");
    [Fact] void should_read_steps_once() => _stepReads.ShouldEqual(1);
    [Fact] void should_show_other_observers_despite_a_failing_read() => _error!.Message.ShouldContain("projection ScenarioProjection: state=Active");
}
