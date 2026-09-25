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

    protected void HasJobs(params JobState[] jobs) =>
        _jobsManager
            .GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(jobs)));

    protected JobState AJob(JobStatus status, ObserverKey? forObserver = null) => new()
    {
        Id = JobId.New(),
        Status = status,
        Request = new CatchUpObserverRequest(forObserver ?? _observerKey, ObserverType.Reactor, EventSequenceNumber.First, [])
    };

    protected async Task StartOrResume(Func<CatchUpObserverRequest, bool>? requestPredicate = null) =>
        _result = await _jobsManager.StartOrResumeObserverJobFor<ICatchUpObserver, CatchUpObserverRequest>(
            NullLogger.Instance,
            _request,
            requestPredicate,
            onAlreadyRunningJob: () => Record(() => _onAlreadyRunningJobWasCalled = true),
            onResume: () => Record(() => _onResumeWasCalled = true),
            onStartNew: () => Record(() => _onStartNewWasCalled = true),
            onResumeRefused: () => Record(() => _onResumeRefusedWasCalled = true));

    static Task Record(Action record)
    {
        record();
        return Task.CompletedTask;
    }
}
