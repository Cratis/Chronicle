// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Receivers.for_WebhookBodyDecoder.when_decoding;

public class a_body_at_the_limit : given.a_kernel_webhook_body
{
    protected override long MaximumBodySize => Encoding.UTF8.GetByteCount(Body);
    Task Because() => Decode();
    [Fact] void should_accept_the_delivery() => _error.ShouldBeNull();
}
