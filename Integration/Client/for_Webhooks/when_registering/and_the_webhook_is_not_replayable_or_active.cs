// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;
using context = Cratis.Chronicle.Integration.for_Webhooks.when_registering.and_the_webhook_is_not_replayable_or_active.context;

namespace Cratis.Chronicle.Integration.for_Webhooks.when_registering;

[Collection(ChronicleCollection.Name)]
public class and_the_webhook_is_not_replayable_or_active(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture)
        : given.webhook_states_after_registering(chronicleInProcessFixture)
    {
        public readonly WebhookId WebhookId = "non-replayable-inactive-webhook";

        public Task Because() => Register(
            (WebhookId, TargetUrl, builder => builder.WithEventType<SomeEvent>().NotReplayable().NotActive()));
    }

    [Fact] void should_store_one_webhook_definition() => Context.StoredWebhooks.Count().ShouldEqual(1);
    [Fact] void should_not_be_replayable() => Context.StoredWebhooks.Single().IsReplayable.ShouldBeFalse();
    [Fact] void should_not_be_active() => Context.StoredWebhooks.Single().IsActive.ShouldBeFalse();
}
