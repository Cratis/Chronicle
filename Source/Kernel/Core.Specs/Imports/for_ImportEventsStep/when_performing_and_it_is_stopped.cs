// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Imports.for_ImportEventsStep;

public class when_performing_and_it_is_stopped : given.an_import_events_step
{
    readonly CancellationTokenSource _stop = new();

    async Task Because()
    {
        await _stop.CancelAsync();
        await Perform(_stop.Token);
    }

    void Destroy() => _stop.Dispose();

    JobStepResult Result => _result.TryGetResult(out var result) ? result : throw new InvalidOperationException("The step threw instead of returning a result.");

    [Fact] void should_report_a_stop_the_job_can_resume_rather_than_a_failure() => (Result.TryGetError(out var error) && error.Cancelled).ShouldBeTrue();
    [Fact] async Task should_not_fail_the_import() => await _import.DidNotReceiveWithAnyArgs().Fail(default!);
    [Fact] void should_leave_the_import_where_it_was() => _record!.State.ShouldEqual(ImportState.Validating);
}
