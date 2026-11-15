// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.InMemory.Observation.Webhooks.for_WebhookSigningKeysStorage.given;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Webhooks.for_WebhookSigningKeysStorage;

public class when_saving_a_key : Specification
{
    WebhookSigningKeysStorage _storage;
    Concepts.Observation.Webhooks.WebhookSigningKey[] _keys;
    Concepts.Observation.Webhooks.WebhookSigningKey[] _result;

    void Establish()
    {
        _storage = new();
        _keys = [signing_keys.Active(), signing_keys.OverlapForWebhook(), signing_keys.Retired()];
    }

    async Task Because()
    {
        foreach (var key in _keys)
        {
            await _storage.Save(key);
        }

        _result = [.. await _storage.GetAll()];
    }

    [Fact] void should_return_every_field_of_every_key() => _result.ShouldContainOnly(_keys);
}
