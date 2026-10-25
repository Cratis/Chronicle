// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver;
using Cratis.Chronicle.Observation.Jobs.for_CatchUpObserver.given;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;
using Orleans.Utilities;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs.given;

public class an_observer_with_zero_key_catch_up_jobs : an_observer_behind_on_a_relevant_event
{
    protected readonly for_Observer.given.RecordingObserverLogger _recordedLogs = new();
    protected readonly List<TestableCatchUpObserver> _preparedJobs = [];
    readonly Queue<(TestKitSilo Silo, TestableCatchUpObserver Job)> _pendingJobs = new();
    readonly Queue<(JobId Id, EventSequenceNumber Handled, EventSequenceNumber Scanned)> _completions = new();
    protected bool _queued = true;

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    void Establish()
    {
        var clientSubscriber = Substitute.For<IClientOwnedObserverSubscriber>();
        _subscriber = clientSubscriber;
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(_nextSequenceNumber));
        _silo.AddProbe(_ => clientSubscriber);
        _appendedEventsQueues.IsSubscribed(_observerKey).Returns(_ => _queued);
        _appendedEventsQueues.Subscribe(_observerKey, Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>())
            .Returns(_ =>
            {
                _queued = true;
                return new AppendedEventsQueueSubscription(_observerKey, 0);
            });
        _appendedEventsQueues.When(_ => _.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>())).Do(_ => _queued = false);
    }

    protected async Task CompletePendingJobs()
    {
        // Completion may enqueue a successor. Give it a separate turn, just as the background start timer does.
        var count = _pendingJobs.Count;
        for (var index = 0; index < count; index++)
        {
            var (silo, job) = _pendingJobs.Dequeue();
            await silo.TimerRegistry.FireAllAsync();
            job.PreparedSteps.ShouldNotBeNull();
            job.PreparedSteps.Count.ShouldEqual(0);
            _preparedJobs.Add(job);
        }
        while (_completions.TryDequeue(out var completed))
        {
            await _observer.CaughtUp(completed.Id, completed.Handled, completed.Scanned);
        }
    }

    protected async Task<Result<JobId, StartJobError>> StartZeroKeyJob(CatchUpObserverRequest request)
    {
        request.ObserverType.ShouldEqual(ObserverType.Reactor);
        var silo = new TestKitSilo();
        var keys = Substitute.For<IObserverKeyIndex>();
        keys.GetKeys(Arg.Any<EventSequenceNumber>()).Returns(new TestObserverKeys([]));
        var indexes = Substitute.For<IObserverKeyIndexes>();
        indexes.GetFor(Arg.Any<ObserverKey>(), Arg.Any<ObserverReaderScope>()).Returns(keys);
        _eventStoreNamespaceStorage.ObserverKeyIndexes.Returns(indexes);

        var steps = Substitute.For<IJobStepStorage>();
        steps.GetForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>())
            .Returns(Cratis.Monads.Catch<IImmutableList<JobStepState>>.Success(ImmutableList<JobStepState>.Empty));
        _eventStoreNamespaceStorage.JobSteps.Returns(steps);
        var jobTypes = Substitute.For<IJobTypes>();
        jobTypes.GetFor(Arg.Any<Type>()).Returns(Result<JobType, IJobTypes.GetForError>.Success(new JobType("CatchUpObserver")));

        silo.AddService(_storage);
        silo.AddService(_observerServiceClient);
        silo.AddService(new JsonSerializerOptions());
        silo.AddService(jobTypes);
        silo.AddService(_loggerFactory);

        // Job resolves its closed generic category, not ILogger<IJob>, when it activates.
        silo.AddService<ILogger<Job<CatchUpObserverRequest, JobStateWithLastHandledEvent>>>(
            new Logger<Job<CatchUpObserverRequest, JobStateWithLastHandledEvent>>(_loggerFactory));
        silo.AddService(NullLogger<ObserverManager<IJobObserver>>.Instance);

        var observer = Substitute.For<IObserver>();
        observer.GetFailedPartitionKeys().Returns(Task.FromResult(Enumerable.Empty<Key>()));
        observer.RegisterCatchingUpPartitions(Arg.Any<IEnumerable<Key>>())
            .Returns(call => _observer.RegisterCatchingUpPartitions(call.Arg<IEnumerable<Key>>()));
        observer.CaughtUp(Arg.Any<JobId>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>())
            .Returns(call =>
            {
                _completions.Enqueue((call.Arg<JobId>(), call.ArgAt<EventSequenceNumber>(1), call.ArgAt<EventSequenceNumber>(2)));
                return Task.CompletedTask;
            });
        silo.AddProbe(_ => observer);

        var id = JobId.New();
        var job = await silo.CreateGrainAsync<TestableCatchUpObserver>(id, new JobKey(_observerKey.EventStore, _observerKey.Namespace));
        var startResult = await job.Start(request);
        startResult.TryGetError(out _).ShouldBeFalse();
        _pendingJobs.Enqueue((silo, job));
        return Result<JobId, StartJobError>.Success(id);
    }
}
