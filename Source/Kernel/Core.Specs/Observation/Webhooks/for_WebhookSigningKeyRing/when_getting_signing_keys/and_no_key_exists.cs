// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookSigningKeyRing.when_getting_signing_keys;

public class and_no_key_exists : given.a_signing_key_ring
{
    IEnumerable<WebhookSigningKey> _result;

    async Task Establish() => await CreateRing();

    async Task Because() => _result = await _ring.GetSigningKeys();

    [Fact] void should_return_one_key() => _result.Count().ShouldEqual(1);
    [Fact] void should_return_an_active_ed25519_key() => (_result.Single().Status, _result.Single().Algorithm).ShouldEqual((WebhookSigningKeyStatus.Active, WebhookSigningAlgorithm.Ed25519));
    [Fact] void should_append_one_generated_event() => _appended.Select(_ => _.Event.GetType()).ShouldEqual([typeof(WebhookSigningKeyGenerated)]);
    [Fact] void should_use_the_key_id_as_event_source() => _appended.Single().EventSource.Value.ShouldEqual(_result.Single().KeyId.Value);
    [Fact] void should_persist_the_key() => State.Keys.ShouldContainOnly(_result);
    [Fact] void should_store_the_private_key_encrypted() => _result.Single().EncryptedSecret.Value.StartsWith("enc:whsk_").ShouldBeTrue();
    [Fact] void should_store_a_private_key_matching_the_public_key() => PublicKeyOf(_result.Single()).ShouldEqual(_result.Single().PublicKey);
    [Fact] void should_not_put_key_material_in_the_event()
    {
        var json = JsonSerializer.Serialize(_appended.Single().Event);
        json.ShouldNotContain("whsk_");
        json.ShouldNotContain("enc:");
    }

    static string PublicKeyOf(WebhookSigningKey key)
    {
        WebhookSigningKeys.TryParsePrivateKey(key.EncryptedSecret.Value["enc:".Length..], out var seed).ShouldBeTrue();
        return WebhookSigningKeys.EncodePublicKey(WebhookEd25519Signing.DerivePublicKey(seed));
    }
}
