// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Receivers.for_WebhookBodyDecoder.when_decoding;

public class a_body_over_the_limit : given.a_kernel_webhook_body
{
    protected override long MaximumBodySize => 5;
    Task Because() => Decode();
    [Fact] void should_refuse_before_parsing() => _error.ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_identify_the_oversized_body() => ((ArgumentOutOfRangeException)_error).ParamName.ShouldEqual("source");
    [Fact] void should_read_only_the_limit_plus_one_byte() => _body.Position.ShouldEqual(6L);
}
