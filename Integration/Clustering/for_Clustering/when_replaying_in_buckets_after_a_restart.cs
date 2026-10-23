// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

[Collection(BucketReplayCollection.Name)]
public class when_replaying_in_buckets_after_a_restart(BucketReplayFixture fixture)
{
    readonly TimeSpan _timeout = TimeSpan.FromSeconds(120);

    [Fact]
    public async Task should_resume_the_original_eight_steps_after_restart_and_rebuild_every_document()
    {
        var ids = Enumerable.Range(0, 24).Select(index => $"bucketed-source-{index}").ToArray();
        var store = fixture.ClientEventStore;
        var handler = store.Projections.GetHandlerFor<ClusteredProjection>();
        await handler.WaitTillActive(_timeout);
        await Append(store, ids);
        fixture.Checkpoint.Arm();
        try
        {
            var jobId = await store.Projections.Replay<ClusteredProjection>();
            await fixture.Checkpoint.Reached.Task.WaitAsync(_timeout);
            var job = (await store.Jobs.GetJob(jobId))!;
            (await job.GetJobSteps()).Count().ShouldEqual(8);
            fixture.Checkpoint.EnteredBucketSteps.ShouldEqual(8);
            fixture.BucketCount = 16;
            await fixture.CrashAndRestart(fixture.Checkpoint.Release);
            await ResumeIfStopped(jobId);
            await fixture.ClientEventStore.Jobs.WaitTillJobCompletesOrIsDeleted(jobId, _timeout);
            foreach (var id in ids)
            {
                var document = await fixture.ClientEventStore.ReadModels.GetInstanceById<ClusteredProjectionReadModel>(id);
                document.Number.ShouldEqual(2);
            }
        }
        finally
        {
            fixture.Checkpoint.Release();
        }
    }

    async Task Append(IEventStore store, string[] ids)
    {
        var last = EventSequenceNumber.Unavailable;
        for (var round = 0; round < 3; round++)
        {
            foreach (var id in ids)
            {
                last = (await store.EventLog.Append(id, new ClusteredEvent(round, ThingId.New(), Priority.High, [], new Address("Street", "City", 12345)))).SequenceNumber;
            }
        }

        await store.Projections.GetHandlerFor<ClusteredProjection>().WaitTillReachesEventSequenceNumber(last, _timeout);
    }

    async Task ResumeIfStopped(JobId id)
    {
        var jobs = fixture.ClientEventStore.Jobs;
        var job = await jobs.GetJob(id);
        if (job?.Status == JobStatus.Stopped) (await jobs.Resume(id)).Accepted.ShouldBeTrue();
        if (job is not null) (await job.GetJobSteps()).Count().ShouldEqual(8);
    }
}
