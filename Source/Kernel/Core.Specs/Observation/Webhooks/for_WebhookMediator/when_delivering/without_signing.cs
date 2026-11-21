// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_delivering;

public class without_signing : given.a_signed_delivery
{
    async Task Because() => _result = await Deliver(null);

    [Fact] void should_deliver() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_emit_signing_headers() => _receiver.Headers.Single().Keys.Any(header => header.StartsWith("webhook-", StringComparison.OrdinalIgnoreCase) || header.Equals("Cratis-Key-Id", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_not_call_the_signer() => _signer.ReceivedCalls().ShouldBeEmpty();
}
