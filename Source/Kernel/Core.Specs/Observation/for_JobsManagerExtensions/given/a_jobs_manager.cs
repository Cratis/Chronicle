// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_JobsManagerExtensions.given;

public class a_jobs_manager : Specification
{
    protected IJobsManager _jobsManager;
    protected CatchUpObserverRequest _request;
    protected ObserverKey _observerKey;

    protected bool _onAlreadyRunningJobWasCalled;
    protected bool _onResumeWasCalled;
    protected bool _onStartNewWasCalled;
    protected bool _onResumeRefusedWasCalled;

    protected JobId _result;

    void Establish()
    {
        _jobsManager = Substitute.For<IJobsManager>();
        _observerKey = new ObserverKey("some-observer", "some-event-store", "some-namespace", EventSequenceId.Log);
        _request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, []);

        HasJobs();
    }

    /// <summary>
    /// Lists the given jobs. A deleted job is gone from the next listing.
    /// </summary>
    /// <param name="jobs">The jobs to list.</param>
    protected void HasJobs(params JobState[] jobs)
    {
        var listed = jobs.ToList();
        _jobsManager
            .GetJobs(Arg.Any<JobQuery>())
            .Returns(_ => Task.FromResult<IImmutableList<JobState>>(listed.ToImmutableList()));
        _jobsManager.Delete(Arg.Any<JobId>()).Returns(call =>
        {
            listed.RemoveAll(_ => _.Id == call.Arg<JobId>());
            return Task.CompletedTask;
        });
    }

    protected JobState AJob(JobStatus status, ObserverKey? forObserver = null) => new()
    {
        Id = JobId.New(),
        Status = status,
        Request = new CatchUpObserverRequest(forObserver ?? _observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
    };

    protected JobState APreparedJob(JobStatus status)
    {
        var job = AJob(status);
        job.StatusChanges = [new JobStatusChanged { Status = JobStatus.StartingSteps, Occurred = DateTimeOffset.UtcNow }];
        return job;
    }

    protected async Task StartOrResume(Func<CatchUpObserverRequest, bool>? requestPredicate = null, ICollection<JobId>? concludedJobs = null, int maxJobsToDrop = 0) =>
        _result = await _jobsManager.StartOrResumeObserverJobFor<ICatchUpObserver, CatchUpObserverRequest>(
            NullLogger.Instance,
            _request,
            requestPredicate,
            onAlreadyRunningJob: () => Record(() => _onAlreadyRunningJobWasCalled = true),
            onResume: () => Record(() => _onResumeWasCalled = true),
            onStartNew: () => Record(() => _onStartNewWasCalled = true),
            onResumeRefused: () => Record(() => _onResumeRefusedWasCalled = true),
            concludedJobs: concludedJobs,
            maxJobsToDrop: maxJobsToDrop);

    static Task Record(Action record)
    {
        record();
        return Task.CompletedTask;
    }
}
