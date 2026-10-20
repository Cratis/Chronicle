// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Webhooks;
using Orleans;
using Orleans.Core.Internal;
using context = Cratis.Chronicle.Integration.for_Webhooks.when_a_receiver_defers_and_the_observer_reactivates.context;

namespace Cratis.Chronicle.Integration.for_Webhooks;

[Collection(ChronicleCollection.Name)]
public class when_a_receiver_defers_and_the_observer_reactivates(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.all_dependencies(chronicleFixture)
    {
        readonly Uri _target = new($"https://retry-after.example.com/{Guid.NewGuid():N}");
        readonly WebhookId _webhook = $"retry-after-{Guid.NewGuid():N}";
        readonly InvokedWebhooks _requests = new();
        IDisposable _lease;
        RetryAfterWebhookReceiver _receiver;
        IObserver _observer;

        public InvokedWebhooks Accepted = new();
        public PartitionDeferral BeforeReactivation;
        public PartitionDeferral AfterReactivation;
        public int AttemptsBeforeReactivation;
        public int AttemptsAfterReactivation;
        public int RequestsWhileDeferred;
        public int SuccessfulPartitionsWhileDeferred;
        public bool HasFailedPartitionsAfterRecovery;
        public string RejectedBody;
        public string RecoveredBody;

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            _receiver ??= new(_requests, Accepted);
            _lease ??= given.TestHttpClientFactory.Instance.Register(_target, _receiver);
        }

        public async Task Establish()
        {
            await EventStore.Webhooks.Register(_webhook, _target.AbsoluteUri, builder => builder.WithEventType<SomeEvent>());
            var registration = await EventStore.Reactors.WaitForHandlerById("$system.Cratis.Chronicle.Observation.Webhooks.WebhookReactor", TimeSpan.FromSeconds(30));
            var tail = (await EventStore.GetEventSequence(EventSequenceId.System).GetTailSequenceNumber()).Value;
            await registration.WaitTillReachesEventSequenceNumber(tail);
            var key = new ObserverKey(_webhook.Value, EventStore.Name.Value, EventStore.Namespace.Value, Concepts.EventSequences.EventSequenceId.Log);
            _observer = Services.GetRequiredService<IGrainFactory>().GetGrain<IObserver>(key.ToString());
            var appended = await EventStore.EventLog.Append("busy", new SomeEvent(42));
            (await appended.WaitForCompletion()).IsSuccess.ShouldBeFalse();
        }

        public async Task Because()
        {
            var failure = (await GetFailures()).Partitions.Single();
            BeforeReactivation = failure.Deferral!;
            AttemptsBeforeReactivation = failure.Attempts.Count();
            RejectedBody = _requests.GetAll().Single().Body;

            await EventStore.EventLog.Append("healthy", new SomeEvent(99));
            await Accepted.WaitForInvocation(1);
            RequestsWhileDeferred = _requests.Count;
            SuccessfulPartitionsWhileDeferred = Accepted.Count;

            await _observer.AsReference<IGrainManagementExtension>().DeactivateOnIdle().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
            await _observer.GetState();
            failure = (await GetFailures()).Partitions.Single();
            AfterReactivation = failure.Deferral!;
            AttemptsAfterReactivation = failure.Attempts.Count();

            await _receiver.WaitForRetry().WaitAsync(TimeSpan.FromSeconds(120));
            Job retryJob;
            try
            {
                retryJob = (await EventStore.Jobs.GetJobs()).Single(job =>
                    job.Type.Value.Contains("RetryFailedPartition", StringComparison.Ordinal) &&
                    job.Details.Value == $"{_webhook}-busy");
            }
            finally
            {
                _receiver.ReleaseRetry();
            }

            var completed = await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(retryJob.Id, TimeSpan.FromSeconds(30));
            completed?.Status.ShouldEqual(JobStatus.CompletedSuccessfully);
            HasFailedPartitionsAfterRecovery = await _observer.HasFailedPartitions();
            RecoveredBody = Accepted.GetAll().Single(delivery => delivery.Body == RejectedBody).Body;
        }

        protected override async Task OnDisposeAsync()
        {
            _receiver?.ReleaseRetry();
            _lease?.Dispose();
            _receiver?.Dispose();
            await base.OnDisposeAsync();
        }

        Task<Concepts.Observation.FailedPartitions> GetFailures() => Services.GetRequiredService<IStorage>()
            .GetEventStore(EventStore.Name.Value).GetNamespace(EventStore.Namespace.Value).FailedPartitions.GetFor((Concepts.Observation.ObserverId)_webhook.Value);
    }

    [Fact] void should_not_consume_a_retry_budget_on_receiver_back_pressure() => Context.AttemptsBeforeReactivation.ShouldEqual(0);
    [Fact] void should_not_consume_a_retry_budget_on_reactivation() => Context.AttemptsAfterReactivation.ShouldEqual(0);
    [Fact] void should_preserve_the_exact_deadline_and_resume_position() => Context.AfterReactivation.ShouldEqual(Context.BeforeReactivation);
    [Fact] void should_not_retry_before_the_deadline() => Context.RequestsWhileDeferred.ShouldEqual(2);
    [Fact] void should_allow_an_unrelated_partition_to_progress() => Context.SuccessfulPartitionsWhileDeferred.ShouldEqual(1);
    [Fact] void should_automatically_resume_the_first_undelivered_batch() => Context.RecoveredBody.ShouldEqual(Context.RejectedBody);
    [Fact] void should_clear_the_deferral_after_success() => Context.HasFailedPartitionsAfterRecovery.ShouldBeFalse();
}
