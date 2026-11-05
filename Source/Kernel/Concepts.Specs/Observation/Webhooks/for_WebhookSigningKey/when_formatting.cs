// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks.for_WebhookSigningKey;

public class when_formatting : Specification
{
    const string Ciphertext = "crk1:abc:SECRET-CIPHERTEXT";

    WebhookSigningKey _key;
    string _result;

    void Establish() => _key = new(
        "the-key-id",
        WebhookSigningAlgorithm.Ed25519,
        WebhookSigningKeyStatus.Active,
        "whpk_public",
        Ciphertext,
        null,
        DateTimeOffset.FromUnixTimeSeconds(100),
        null,
        null);

    void Because() => _result = _key.ToString();

    [Fact] void should_contain_the_key_id() => _result.ShouldContain("the-key-id");
    [Fact] void should_not_contain_the_ciphertext() => _result.ShouldNotContain(Ciphertext);
    [Fact] void should_not_contain_the_encrypted_value_at_all() => _result.ShouldNotContain("SECRET");
}
