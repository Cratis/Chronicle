// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_ChronicleServerStartupTask.when_executing;

/// <summary>
/// A silo of a release that cannot decode this release's grain calls answers the same way every time, so the step
/// fails at once rather than spending the retry budget on a cluster that will not change until it is stopped (#5272).
/// </summary>
public class and_a_sibling_silo_runs_an_incompatible_release : given.a_startup_task
{
    Exception _error;

    void Establish() =>
        _jobsManager.Rehydrate()
            .Returns(_ => Task.FromException(new TypeLoadException("Unable to resolve type alias \"(\"inv\",[Orleans.Runtime.GrainReference],[Cratis.Orleans.Jobs.IJobsManager,Cratis.Orleans],\"00000000\")\".")));

    async Task Because() => _error = await Catch.Exception(Execute);

    [Fact] void should_fail() => _error.ShouldBeOfExactType<TypeLoadException>();
    [Fact] async Task should_not_have_tried_again() => await _jobsManager.Received(1).Rehydrate();
}
