// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookDeliverySigner.when_signing;

public class with_a_malformed_private_key : given.a_delivery_signer
{
    void Establish() => _encryption.Decrypt("encrypted-active").Returns("whsk_not-base64-private-material");
    Task Because() => Sign();
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookSigningUnavailable>();
    [Fact] void should_not_expose_key_material() => _error!.ToString().Contains("not-base64-private-material", StringComparison.Ordinal).ShouldBeFalse();
}
