// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_delivering;

public class with_spoofed_signing_headers : given.a_signed_delivery
{
    void Establish()
    {
        foreach (var name in new[] { "wEbHoOk-Id", "WEBHOOK-TIMESTAMP", "webhook-signature", "cratis-key-id" })
            _client.DefaultRequestHeaders.TryAddWithoutValidation(name, new[] { "spoofed", "duplicate" });
    }

    async Task Because() => _result = await Deliver(_identity);

    [Fact] void should_replace_all_untrusted_values() => _receiver.Headers.Single().Values.SelectMany(values => values).Any(value => value == "spoofed" || value == "duplicate").ShouldBeFalse();
    [Fact] void should_emit_one_value_for_each_signing_header() => new[] { "webhook-id", "webhook-timestamp", "webhook-signature", "Cratis-Key-Id" }.All(name => _receiver.Headers.Single()[name].Length == 1).ShouldBeTrue();
    [Fact] void should_verify() => _receiver.Verifies(0).ShouldBeTrue();
}
