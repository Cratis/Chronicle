// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Observation.for_ReplayObserver.given;

public class a_replay_command : Specification
{
    protected readonly ObserverKey _key = new("some-observer", "some-event-store", "some-namespace", "inbox-source");
    protected readonly CommandScenario<ReplayObserver> _scenario = ChronicleCommandScenario.For<ReplayObserver>();
    protected readonly JobId _acceptedJob = JobId.New();
    protected IGrainFactory _grainFactory;
    protected IObserver _observer;
    protected IJobsManager _jobsManager;
    protected IObserverDefinitionsStorage _definitions;
    protected CommandResult _result;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _definitions = Substitute.For<IObserverDefinitionsStorage>();
        storage.GetEventStore(_key.EventStore).Returns(eventStoreStorage);
        eventStoreStorage.Observers.Returns(_definitions);
        _definitions.Get(_key.ObserverId).Returns(new ObserverDefinition(
            _key.ObserverId, [], _key.EventSequenceId, ObserverType.Reactor, ObserverOwner.Client, true));
        _grainFactory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);

        // The command is accepted as a job by the jobs manager of the namespace; nothing is outstanding by default.
        _jobsManager = Substitute.For<IJobsManager>();
        _grainFactory.GetGrain<IJobsManager>(Arg.Any<long>(), Arg.Any<string>()).Returns(_jobsManager);
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager.Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(Arg.Any<ObserverOperatorCommandRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_acceptedJob)));
        _scenario.Services.AddSingleton(storage);
        _scenario.Services.AddSingleton(_grainFactory);
    }

    async Task Destroy() => await _scenario.DisposeAsync();
}
