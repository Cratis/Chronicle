// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers.for_WebhookBodyDecoder.when_decoding;

public class without_a_target : given.a_kernel_webhook_body
{
    private protected override DeliveryTarget? Target => null;
    Task Because() => Decode();
    [Fact] void should_refuse_the_delivery() => _error.ShouldBeOfExactType<MalformedDelivery>();
    [Fact] void should_not_read_the_body() => _body.Position.ShouldEqual(0L);
}
