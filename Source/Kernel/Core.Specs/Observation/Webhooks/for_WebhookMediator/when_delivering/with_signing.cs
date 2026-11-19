// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_delivering;

public class with_signing : given.a_signed_delivery
{
    async Task Because() => _result = await Deliver(_identity);

    [Fact] void should_deliver() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_sign_the_exact_received_bytes() => _receiver.Verifies(0).ShouldBeTrue();
    [Fact] void should_keep_the_existing_json_shape() => _receiver.Bodies.Single().ShouldEqual(JsonSerializer.SerializeToUtf8Bytes(new EventsToObserve("partition", _events), _jsonOptions));
    [Fact] void should_keep_the_existing_content_type() => _receiver.ContentTypes.ShouldContainOnly("application/json; charset=utf-8");
    [Fact] void should_emit_the_advisory_key_id() => _receiver.Headers.Single()["Cratis-Key-Id"].ShouldContainOnly("active-key");
    [Fact] void should_not_log_the_private_seed() => _logs.Entries.All(entry => !entry.Text.Contains(Convert.ToBase64String(_seed), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_log_the_signature() => _logs.Entries.All(entry => !entry.Text.Contains(_receiver.Headers.Single()["webhook-signature"].Single(), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_put_the_seed_in_headers() => string.Join(' ', _receiver.Headers.Single().SelectMany(header => header.Value)).Contains(Convert.ToBase64String(_seed), StringComparison.Ordinal).ShouldBeFalse();
}
