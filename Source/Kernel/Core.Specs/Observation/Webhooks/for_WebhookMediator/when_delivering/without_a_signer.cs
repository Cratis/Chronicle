// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_delivering;

public class without_a_signer : given.a_signed_delivery
{
    void Establish() => _mediator = new(_httpClientFactory, _jsonOptions, logger: _logs);
    async Task Because() => _result = await Deliver(_identity);
    [Fact] void should_fail_closed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_never_send_unsigned() => _receiver.Bodies.ShouldBeEmpty();
}
