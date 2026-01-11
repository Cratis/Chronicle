// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using context = Cratis.Chronicle.MongoDB.Integration.Jobs.for_JobStorage.when_saving_and_reading_job.context;

namespace Cratis.Chronicle.MongoDB.Integration.Jobs.for_JobStorage;

[Collection(MongoDBCollection.Name)]
public class when_saving_and_reading_job(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_job_storage(fixture)
    {
        public JobId JobId;
        public JobState OriginalState = default!;
        public JobState RetrievedState = default!;

        async Task Establish()
        {
            JobId = JobId.New();
            OriginalState = new JobState
            {
                Id = JobId,
                Details = "Test Job",
                Type = typeof(IntegrationJobRequest),
                Status = JobStatus.Running,
                Created = DateTimeOffset.UtcNow,
                StatusChanges =
                [
                    new JobStatusChanged
                    {
                        Status = JobStatus.Running,
                        Occurred = DateTimeOffset.UtcNow,
                        ExceptionMessages = [],
                        ExceptionStackTrace = string.Empty
                    }
                ],
                Progress = new JobProgress { TotalSteps = 100, SuccessfulSteps = 50, FailedSteps = 0 },
                Request = new IntegrationJobRequest("Test Request", 42, DateTimeOffset.UtcNow)
            };
        }

        async Task Because()
        {
            var saveResult = await _storage.Save(JobId, OriginalState);
            saveResult.Match(
                _ => true,
                error => throw new Exception($"Failed to save: {error}"),
                exception => throw exception);

            var readResult = await _storage.GetJob(JobId);
            RetrievedState = readResult.Match(
                state => state,
                error => throw new Exception($"Failed to read: {error}"),
                exception => throw exception);
        }
    }

    [Fact] void should_retrieve_job_with_same_id() => Context.RetrievedState.Id.ShouldEqual(Context.OriginalState.Id);
    [Fact] void should_retrieve_job_with_same_details() => Context.RetrievedState.Details.ShouldEqual(Context.OriginalState.Details);
    [Fact] void should_retrieve_job_with_same_type() => Context.RetrievedState.Type.ShouldEqual(Context.OriginalState.Type);
    [Fact] void should_retrieve_job_with_same_status() => Context.RetrievedState.Status.ShouldEqual(Context.OriginalState.Status);
    [Fact] void should_retrieve_job_with_same_progress() => Context.RetrievedState.Progress.TotalSteps.ShouldEqual(Context.OriginalState.Progress.TotalSteps);
    [Fact] void should_retrieve_request_as_correct_type() => Context.RetrievedState.Request.ShouldBeOfExactType<IntegrationJobRequest>();
    [Fact] void should_retrieve_request_with_same_name() => ((IntegrationJobRequest)Context.RetrievedState.Request).Name.ShouldEqual("Test Request");
    [Fact] void should_retrieve_request_with_same_count() => ((IntegrationJobRequest)Context.RetrievedState.Request).Count.ShouldEqual(42);
}
