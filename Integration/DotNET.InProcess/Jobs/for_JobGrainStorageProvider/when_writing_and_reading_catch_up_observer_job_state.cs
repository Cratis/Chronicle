// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using context = Cratis.Chronicle.InProcess.Integration.Jobs.for_JobGrainStorageProvider.when_writing_and_reading_catch_up_observer_job_state.context;

namespace Cratis.Chronicle.InProcess.Integration.Jobs.for_JobGrainStorageProvider;

[Collection(ChronicleCollection.Name)]
public class when_writing_and_reading_catch_up_observer_job_state(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : given.a_job_grain_storage_provider(fixture)
    {
        public JobStateWithLastHandledEvent OriginalState = default!;
        public JobStateWithLastHandledEvent RetrievedState = default!;
        public GrainId JobGrainId = default!;
        public GrainState<JobStateWithLastHandledEvent> GrainState = default!;
        public string GrainStateName = "JobState";

        void Establish()
        {
            base.Establish();

            var jobId = JobId.New();
            var request = CreateCatchUpObserverRequest();

            OriginalState = new JobStateWithLastHandledEvent
            {
                Id = jobId,
                Details = $"Catch-up for observer {request.ObserverKey.ObserverId}",
                Type = typeof(CatchUpObserver),
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
                Request = request,
                LastHandledEventSequenceNumber = new EventSequenceNumber(150),
                HandledAllEvents = false
            };

            JobGrainId = CreateGrainId(Guid.Parse(OriginalState.Id.Value.ToString()));

            GrainState = new GrainState<JobStateWithLastHandledEvent>
            {
                State = OriginalState,
                ETag = "*"
            };
        }

        async Task Because()
        {
            // Write the job state
            await _provider.WriteStateAsync(GrainStateName, JobGrainId, GrainState);

            // Read it back
            var readGrainState = new GrainState<JobStateWithLastHandledEvent>();
            await _provider.ReadStateAsync(GrainStateName, JobGrainId, readGrainState);

            RetrievedState = readGrainState.State;
        }
    }

    [Fact] void should_preserve_job_id() => Context.RetrievedState.Id.ShouldEqual(Context.OriginalState.Id);
    [Fact] void should_preserve_details() => Context.RetrievedState.Details.ShouldEqual(Context.OriginalState.Details);
    [Fact] void should_preserve_job_type() => Context.RetrievedState.Type.ShouldEqual(Context.OriginalState.Type);
    [Fact] void should_preserve_status() => Context.RetrievedState.Status.ShouldEqual(Context.OriginalState.Status);
    [Fact] void should_preserve_request_as_catch_up_observer_request() => Context.RetrievedState.Request.ShouldBeOfExactType<CatchUpObserverRequest>();
    [Fact]
    void should_preserve_observer_key()
    {
        var originalRequest = (CatchUpObserverRequest)Context.OriginalState.Request;
        var retrievedRequest = (CatchUpObserverRequest)Context.RetrievedState.Request;
        retrievedRequest.ObserverKey.ShouldEqual(originalRequest.ObserverKey);
    }
    [Fact]
    void should_preserve_observer_type()
    {
        var originalRequest = (CatchUpObserverRequest)Context.OriginalState.Request;
        var retrievedRequest = (CatchUpObserverRequest)Context.RetrievedState.Request;
        retrievedRequest.ObserverType.ShouldEqual(originalRequest.ObserverType);
    }
    [Fact]
    void should_preserve_from_event_sequence_number()
    {
        var originalRequest = (CatchUpObserverRequest)Context.OriginalState.Request;
        var retrievedRequest = (CatchUpObserverRequest)Context.RetrievedState.Request;
        retrievedRequest.FromEventSequenceNumber.ShouldEqual(originalRequest.FromEventSequenceNumber);
    }
    [Fact] void should_preserve_last_handled_event_sequence_number() => Context.RetrievedState.LastHandledEventSequenceNumber.ShouldEqual(Context.OriginalState.LastHandledEventSequenceNumber);
    [Fact] void should_preserve_handled_all_events_flag() => Context.RetrievedState.HandledAllEvents.ShouldEqual(Context.OriginalState.HandledAllEvents);
}
