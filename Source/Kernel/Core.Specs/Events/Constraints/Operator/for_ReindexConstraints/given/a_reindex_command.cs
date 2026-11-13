// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Events.Constraints.Operator.for_ReindexConstraints.given;

public class a_reindex_command : Specification
{
    protected readonly CommandScenario<ReindexConstraints> _scenario = ChronicleCommandScenario.For<ReindexConstraints>();
    protected IGrainFactory _grainFactory;
    protected IConstraints _constraints;
    protected IEventSequence _sequence;
    protected IJobsManager _jobs;
    protected IConstraintIndexes _indexes;
    protected readonly JobId _jobId = JobId.New();
    protected List<IConstraintDefinition> _definitions;
    protected ReindexConstraintsRequest _request;
    protected CommandResult _result;

    void Establish()
    {
        _definitions = [new UniqueConstraintDefinition("unique", [new("Registered", ["email"])]), new ClosesStreamConstraintDefinition("closing", ["Closed"], ClosedStreamDimensions.EventSourceId, []), new UniqueEventTypeConstraintDefinition("once", ["Happened"])];
        _grainFactory = Substitute.For<IGrainFactory>();
        _constraints = Substitute.For<IConstraints>();
        _sequence = Substitute.For<IEventSequence>();
        _jobs = Substitute.For<IJobsManager>();
        _constraints.GetDefinitions().Returns(_definitions);
        _grainFactory.GetGrain<IConstraints>(Arg.Any<string>(), Arg.Any<string>()).Returns(_constraints);
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>(), Arg.Any<string>()).Returns(_sequence);
        _grainFactory.GetGrain<IJobsManager>(Arg.Any<long>(), Arg.Any<string>()).Returns(_jobs);
        _jobs.GetJobs(Arg.Any<JobQuery>()).Returns(ImmutableList<JobState>.Empty);
        _jobs.Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Do<ReindexConstraintsRequest>(request => _request = request)).Returns(Result<JobId, StartJobError>.Success(_jobId));
        _scenario.Services.AddSingleton(_grainFactory);
        var storage = Substitute.For<IStorage>();
        storage.HasEventStore(Arg.Any<Concepts.EventStoreName>()).Returns(true);
        _scenario.Services.AddSingleton(storage);
        _indexes = new ConstraintIndexes(_grainFactory, storage, NullLogger<ConstraintIndexes>.Instance);
        _scenario.Services.AddSingleton(_indexes);
    }

    async Task Destroy() => await _scenario.DisposeAsync();
}
