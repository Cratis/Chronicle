// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Webhooks.for_WebhookDefinitionsStorage.when_saving;

public class with_pinned_generations : Specification
{
    WebhookDefinitionsStorage _storage;
    WebhookDefinition _definition;
    WebhookDefinition _result;

    void Establish()
    {
        _storage = new();
        _definition = new("webhook", WebhookOwner.Client, "log", [new EventType("customer", 3)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()))
        {
            GenerationDelivery = EventGenerationDelivery.Pinned
        };
    }

    async Task Because()
    {
        await _storage.Save(_definition);
        _result = await _storage.Get(_definition.Identifier);
    }

    [Fact] void should_preserve_the_delivery_policy() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
    [Fact] void should_preserve_exact_pins() => _result.EventTypes.ShouldContainOnly(_definition.EventTypes);
    void Destroy() => _storage.Dispose();
}
