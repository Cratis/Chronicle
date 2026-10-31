// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Webhooks.for_WebhookDefinitionsStorage.when_saving;

public class with_a_legacy_definition : Specification
{
    WebhookDefinitionsStorage _storage;
    WebhookDefinition _result;
    void Establish() => _storage = new();
    async Task Because()
    {
        var definition = new WebhookDefinition("webhook", WebhookOwner.Client, "log", [new EventType("customer", 3)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()));
        await _storage.Save(definition);
        _result = await _storage.Get(definition.Identifier);
    }
    [Fact] void should_default_to_compatibility() => _result.GenerationDelivery.ShouldEqual(EventGenerationDelivery.Compatibility);
    [Fact] void should_preserve_the_declared_generation() => _result.EventTypes.Single().Generation.Value.ShouldEqual(3u);
    void Destroy() => _storage.Dispose();
}
