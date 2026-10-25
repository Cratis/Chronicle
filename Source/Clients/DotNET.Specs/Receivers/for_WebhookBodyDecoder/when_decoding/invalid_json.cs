// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers.for_WebhookBodyDecoder.when_decoding;

public class invalid_json : given.a_kernel_webhook_body
{
    protected override string Body => "{secret-content";
    Task Because() => Decode();
    [Fact] void should_refuse_the_delivery() => _error.ShouldBeOfExactType<MalformedDelivery>();
    [Fact] void should_not_expose_the_payload() => _error.Message.Contains("secret-content", StringComparison.Ordinal).ShouldBeFalse();
}
