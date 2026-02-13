// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Webhooks;
using context = Cratis.Chronicle.InProcess.Integration.for_Webhooks.when_registering.and_webhook_not_already_registered.context;

namespace Cratis.Chronicle.InProcess.Integration.for_Webhooks.when_registering;

[Collection(ChronicleCollection.Name)]
public class and_webhook_not_already_registered(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture)
        : given.webhook_states_after_registering(chronicleInProcessFixture)
    {
        public readonly WebhookId WebhookId = "some-webhook";

        public Task Because()
        {
            return Register(
                (WebhookId, TargetUrl, builder => builder.WithEventType<SomeEvent>()));
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
}