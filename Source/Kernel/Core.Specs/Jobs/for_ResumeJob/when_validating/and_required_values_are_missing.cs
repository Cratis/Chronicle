// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Jobs.for_ResumeJob.when_validating;

public class and_required_values_are_missing : Specification
{
    readonly CommandScenario<ResumeJob> _scenario = new();
    CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);

        var jobsManager = Substitute.For<IJobsManager>();
        jobsManager.GetAllJobs().Returns(new List<JobState>().ToImmutableList());
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IJobsManager>(Arg.Any<long>(), Arg.Any<string>()).Returns(jobsManager);
        _scenario.Services.AddSingleton(grainFactory);
    }

    async Task Because() => _result = await _scenario.Validate(new ResumeJob(string.Empty, string.Empty, Guid.Empty));

    [Fact] void should_not_be_successful() => _result.ShouldNotBeSuccessful();
    [Fact] void should_have_validation_errors() => _result.ShouldHaveValidationErrors();
}
