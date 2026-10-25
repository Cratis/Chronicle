// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers.for_WebhookBodyDecoder.when_decoding;

public class a_body_without_events : given.a_kernel_webhook_body
{
    protected override string Body => "{\"partition\":\"order-42\"}";
    Task Because() => Decode();
    [Fact] void should_refuse_the_delivery() => _error.ShouldBeOfExactType<MalformedDelivery>();
}
