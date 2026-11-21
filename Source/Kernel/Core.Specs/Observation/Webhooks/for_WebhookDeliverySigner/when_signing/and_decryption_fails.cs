// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Signing.for_WebhookDeliverySigner.when_signing;

public class and_decryption_fails : given.a_delivery_signer
{
    void Establish() => _encryption.Decrypt("encrypted-active").Returns<string>(_ => throw new InvalidOperationException(WebhookSigningKeys.EncodePrivateKey(Seed)));
    Task Because() => Sign();
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WebhookSigningUnavailable>();
    [Fact] void should_not_expose_the_cause() => _error!.InnerException.ShouldBeNull();
    [Fact] void should_not_expose_key_material() => _error!.ToString().Contains(Convert.ToBase64String(Seed), StringComparison.Ordinal).ShouldBeFalse();
}
