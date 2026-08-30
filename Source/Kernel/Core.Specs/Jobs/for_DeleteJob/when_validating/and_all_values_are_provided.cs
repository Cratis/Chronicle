// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Jobs.for_DeleteJob.when_validating;

public class and_all_values_are_provided : Specification
{
    readonly CommandScenario<DeleteJob> _scenario = ChronicleCommandScenario.For<DeleteJob>();
    readonly Concepts.Jobs.JobId _jobId = Concepts.Jobs.JobId.New();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);

        var jobsManager = Substitute.For<IJobsManager>();
        jobsManager.GetAllJobs().Returns(new List<JobState> { new() { Id = _jobId } }.ToImmutableList());
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IJobsManager>(Arg.Any<long>(), Arg.Any<string>()).Returns(jobsManager);
        _scenario.Services.AddSingleton(grainFactory);
    }

    async Task Because() => _result = await _scenario.Validate(new DeleteJob("some-event-store", "some-namespace", _jobId.Value));

    [Fact] void should_be_valid() => _result.ShouldBeValid();
}
