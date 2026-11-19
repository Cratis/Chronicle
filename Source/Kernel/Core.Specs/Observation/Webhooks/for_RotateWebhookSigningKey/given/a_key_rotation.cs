// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_RotateWebhookSigningKey.given;

public class a_key_rotation : Signing.for_WebhookDeliverySigner.given.a_delivery_signer
{
    protected RotateWebhookSigningKey _command;
    protected WebhookSigningKeyOverlap? _overlap;

    void Establish()
    {
        _command = new("store");
        _ring.Rotate(Arg.Any<WebhookSigningKeyOverlap>()).Returns(call =>
        {
            _overlap = call.Arg<WebhookSigningKeyOverlap>();
            return _keys[0];
        });
    }
}
