// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookDeliverySigner.when_signing;

public class with_an_unsupported_algorithm : given.a_delivery_signer
{
    void Establish() => _keys = [_keys.Single() with { Algorithm = WebhookSigningAlgorithm.HmacSha256 }];
    Task Because() => Sign();
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookSigningUnavailable>();
    [Fact] void should_not_return_headers() => _signature.ShouldBeNull();
}
