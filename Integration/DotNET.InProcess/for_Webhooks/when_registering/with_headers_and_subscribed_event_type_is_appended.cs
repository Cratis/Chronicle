// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Webhooks;
using context = Cratis.Chronicle.InProcess.Integration.for_Webhooks.when_registering.with_headers_and_subscribed_event_type_is_appended.context;

namespace Cratis.Chronicle.InProcess.Integration.for_Webhooks.when_registering;

[Collection(ChronicleCollection.Name)]
public class with_headers_and_subscribed_event_type_is_appended(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture)
        : given.webhook_states_after_registering(chronicleInProcessFixture)
    {
        public readonly WebhookId WebhookId = "some-webhook";

        public Task Establish()
        {
            return Register(
                (WebhookId, TargetUrl, builder => builder.WithEventType<SomeEvent>().WithHeader("some-header", "some-value")));
        }

        public async Task Because()
        {
            await EventStore.EventLog.Append("some-event-source", new SomeEvent(42));
            await InvokedWebhooks.WaitForInvocation(1);
        }
    }

    [Fact]
    void should_have_stored_one_webhook_definition() => Context.StoredWebhooks.Count().ShouldEqual(1);

    [Fact]
    void should_have_correct_webhook_id() => Context.StoredWebhooks.Single().Identifier.Value.ShouldEqual(Context.WebhookId.Value);

    [Fact]
    void should_have_correct_event_sequence_id() => Context.StoredWebhooks.Single().EventSequenceId.Value.ShouldEqual(EventSequenceId.Log.Value);

    [Fact]
    void should_be_active() => Context.StoredWebhooks.Single().IsActive.ShouldBeTrue();

    [Fact]
    void should_be_replayable() => Context.StoredWebhooks.Single().IsReplayable.ShouldBeTrue();

    [Fact]
    void should_have_the_correct_target_url() => Context.StoredWebhooks.Single().Target.Url.Value.ShouldEqual(Context.TargetUrl.Value);

    [Fact]
    async Task should_have_no_failing_partitions_for_observer()
    {
        var failedPartitions = await Context.GetEventStoreNamespaceStorage().FailedPartitions.GetFor(Context.WebhookId.Value);
        failedPartitions.HasFailedPartitions.ShouldBeFalse();
    }

    [Fact]
    void should_have_invoked_webhook() => Context.InvokedWebhooks.Count.ShouldEqual(1);

    [Fact]
    void should_have_invoked_webhook_with_the_correct_headers() => Context.InvokedWebhooks.GetAll().Single().Headers
        .ShouldContainOnly(new KeyValuePair<string, string>("some-header", "some-value"));
}
