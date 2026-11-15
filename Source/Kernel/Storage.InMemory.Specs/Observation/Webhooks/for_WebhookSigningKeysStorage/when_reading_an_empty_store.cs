// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Observation.Webhooks.for_WebhookSigningKeysStorage;

public class when_reading_an_empty_store : Specification
{
    WebhookSigningKeysStorage _storage;
    IEnumerable<Concepts.Observation.Webhooks.WebhookSigningKey> _result;

    void Establish() => _storage = new();

    async Task Because() => _result = await _storage.GetAll();

    [Fact] void should_return_no_keys() => _result.ShouldBeEmpty();
}
