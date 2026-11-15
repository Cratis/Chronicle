// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.InMemory.Observation.Webhooks.for_WebhookSigningKeysStorage.given;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Webhooks.for_WebhookSigningKeysStorage;

public class when_retiring_a_key : Specification
{
    WebhookSigningKeysStorage _storage;
    Concepts.Observation.Webhooks.WebhookSigningKey _result;

    void Establish() => _storage = new();

    async Task Because()
    {
        await _storage.Save(signing_keys.Active("key"));
        await _storage.Save(signing_keys.Retired("key"));
        _result = (await _storage.GetAll()).Single();
    }

    [Fact] void should_have_no_encrypted_secret() => _result.EncryptedSecret.Value.ShouldBeEmpty();
    [Fact] void should_be_retired() => _result.Status.ShouldEqual(Concepts.Observation.Webhooks.WebhookSigningKeyStatus.Retired);
}
