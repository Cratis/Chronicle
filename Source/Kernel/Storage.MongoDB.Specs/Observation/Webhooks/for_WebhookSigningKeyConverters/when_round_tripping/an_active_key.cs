// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks.for_WebhookSigningKeyConverters.when_round_tripping;

public class an_active_key : Specification
{
    Concepts.Observation.Webhooks.WebhookSigningKey _key;
    Concepts.Observation.Webhooks.WebhookSigningKey _result;

    void Establish() => _key = new("active", WebhookSigningAlgorithm.Ed25519, WebhookSigningKeyStatus.Active, "whpk_a", "enc:whsk_a", null, DateTimeOffset.FromUnixTimeSeconds(100), null, null);

    void Because() => _result = _key.ToMongoDB().ToKernel();

    [Fact] void should_preserve_every_field() => _result.ShouldEqual(_key);
}
