// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_StalledObserverJobs.when_finding_stalled_jobs;

public class and_retrying_jobs_have_identical_timestamps : given.a_stalled_job_tracker
{
    JobState[] _jobs;
    readonly List<IReadOnlyList<JobId>> _rounds = [];

    void Establish()
    {
        _jobs = Enumerable.Range(0, (StalledObserverJobs.MaxResumesPerTick * 2) + 2)
            .Select(_ => new JobState { Id = JobId.New(), Status = JobStatus.Running }).ToArray();
        _tracker.Find(_jobs, _now);
        foreach (var job in _jobs)
        {
            _tracker.RetryOnNextTick(job.Id);
        }
    }

    void Because()
    {
        for (var tick = 0; tick < 3; tick++)
        {
            var selected = _tracker.Find(_jobs.Reverse(), _now + TimeSpan.FromSeconds(tick));
            _rounds.Add(selected);
            foreach (var id in selected)
            {
                _tracker.RetryOnNextTick(id);
            }
        }
    }

    [Fact] void should_keep_the_cap_on_every_tick() => _rounds.ShouldEachConformTo(_ => _.Count == StalledObserverJobs.MaxResumesPerTick);
    [Fact] void should_attempt_every_job_within_three_ticks() => _rounds.SelectMany(_ => _).Distinct().Count().ShouldEqual(_jobs.Length);
    [Fact] void should_use_first_seen_order_for_equal_timestamps() => _rounds[0].SequenceEqual(_jobs.Take(StalledObserverJobs.MaxResumesPerTick).Select(_ => _.Id)).ShouldBeTrue();
    [Fact] void should_prioritize_the_next_unattempted_group() => _rounds[1].SequenceEqual(_jobs.Skip(StalledObserverJobs.MaxResumesPerTick).Take(StalledObserverJobs.MaxResumesPerTick).Select(_ => _.Id)).ShouldBeTrue();
    [Fact] void should_attempt_the_last_jobs_before_retrying() => _rounds[2].Take(2).SequenceEqual(_jobs.TakeLast(2).Select(_ => _.Id)).ShouldBeTrue();
}
