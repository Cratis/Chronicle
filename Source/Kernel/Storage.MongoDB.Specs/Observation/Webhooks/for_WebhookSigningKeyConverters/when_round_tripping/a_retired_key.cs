// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookSigningKeyConverters.when_round_tripping;

public class a_retired_key : Specification
{
    Concepts.Observation.Webhooks.WebhookSigningKey _key;
    Concepts.Observation.Webhooks.WebhookSigningKey _result;

    void Establish() => _key = new("retired", WebhookSigningAlgorithm.Ed25519, WebhookSigningKeyStatus.Retired, "whpk_c", EncryptedWebhookSigningSecret.Empty, null, DateTimeOffset.FromUnixTimeSeconds(120), DateTimeOffset.FromUnixTimeSeconds(200), DateTimeOffset.FromUnixTimeSeconds(300));

    void Because() => _result = _key.ToMongoDB().ToKernel();

    [Fact] void should_preserve_every_field() => _result.ShouldEqual(_key);
}
