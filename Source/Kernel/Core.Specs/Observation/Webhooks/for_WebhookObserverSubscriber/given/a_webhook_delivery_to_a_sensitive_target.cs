// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.given;

public class a_webhook_delivery_to_a_sensitive_target : a_webhook_delivery
{
    protected override WebhookTarget Target => new(SensitiveTarget.WithCredentials(SensitiveTarget.UniqueHost()), WebhookAuthorization.None, new Dictionary<string, string>());

    void Establish() => _httpClient.BaseAddress = new Uri(_webhookDefinition.Target.Url);
}
