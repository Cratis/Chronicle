// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.InMemory.Observation.Webhooks.for_WebhookSigningKeysStorage.given;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Webhooks.for_WebhookSigningKeysStorage;

public class when_saving_the_same_key_twice : Specification
{
    WebhookSigningKeysStorage _storage;
    Concepts.Observation.Webhooks.WebhookSigningKey _latest;
    Concepts.Observation.Webhooks.WebhookSigningKey[] _result;

    void Establish()
    {
        _storage = new();
        _latest = signing_keys.Active() with { Status = Concepts.Observation.Webhooks.WebhookSigningKeyStatus.Overlap, OverlapEndsAt = DateTimeOffset.FromUnixTimeSeconds(500) };
    }

    async Task Because()
    {
        await _storage.Save(signing_keys.Active());
        await _storage.Save(_latest);
        _result = [.. await _storage.GetAll()];
    }

    [Fact] void should_hold_one_key() => _result.Length.ShouldEqual(1);
    [Fact] void should_hold_the_latest_values() => _result[0].ShouldEqual(_latest);
}
