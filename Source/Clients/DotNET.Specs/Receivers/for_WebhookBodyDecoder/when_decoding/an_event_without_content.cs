// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Receivers.for_WebhookBodyDecoder.when_decoding;

public class an_event_without_content : given.a_kernel_webhook_body
{
    protected override string Body
    {
        get
        {
            var body = JsonNode.Parse(Serialize())!;
            body["events"]![0]!.AsObject().Remove("content");
            return body.ToJsonString();
        }
    }
    Task Because() => Decode();
    [Fact] void should_refuse_the_delivery() => _error.ShouldBeOfExactType<MalformedDelivery>();
}
