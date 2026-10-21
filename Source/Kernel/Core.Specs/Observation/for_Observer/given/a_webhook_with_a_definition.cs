// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_webhook_with_a_definition : a_webhook_observer
{
    protected WebhookDefinition _webhookDefinition;

    void Establish()
    {
        _webhookDefinition = new((WebhookId)_observerId.Value, WebhookOwner.Kernel, EventSequenceId.Log, ["Event1", "Event2"], new("https://receiver.test/events", WebhookAuthorization.None, new Dictionary<string, string>()), true);
        _eventStoreStorage.Webhooks.Has(_webhookDefinition.Identifier).Returns(true);
        _eventStoreStorage.Webhooks.Get(_webhookDefinition.Identifier).Returns(_ => _webhookDefinition);
    }
}
