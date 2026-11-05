// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks.for_WebhookSigningKeyConverters.when_round_tripping;

public class an_overlap_key_for_a_webhook : Specification
{
    Concepts.Observation.Webhooks.WebhookSigningKey _key;
    Concepts.Observation.Webhooks.WebhookSigningKey _result;

    void Establish() => _key = new("overlap", WebhookSigningAlgorithm.HmacSha256, WebhookSigningKeyStatus.Overlap, string.Empty, "enc:whsec_b", "orders", DateTimeOffset.FromUnixTimeSeconds(110), DateTimeOffset.FromUnixTimeSeconds(200), null);

    void Because() => _result = _key.ToSql().ToKernel();

    [Fact] void should_preserve_every_field() => _result.ShouldEqual(_key);
}
