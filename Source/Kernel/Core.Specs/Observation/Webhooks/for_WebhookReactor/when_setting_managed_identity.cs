// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookReactor;

public class when_setting_managed_identity : given.a_webhook_reactor
{
    async Task Because() => await _reactor.ManagedIdentityAuthorizationSet(new("client", "api://resource/.default"), EventContext.EmptyWithEventSourceId("webhook") with { EventStore = "store" });

    [Fact] async Task should_fold_the_authorization() => await _webhooksManager.Received(1).SetAuthorization("webhook", Arg.Is<WebhookAuthorization>(_ => _.IsT4 && _.AsT4.ClientId.Value == "client" && _.AsT4.Scope.Value == "api://resource/.default"));
}
