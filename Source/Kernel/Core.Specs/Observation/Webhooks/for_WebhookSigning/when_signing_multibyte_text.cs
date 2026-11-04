// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigning;

public class when_signing_multibyte_text : given.a_signing_fixture
{
    string _signature;

    void Establish() => _payload = "{\"text\":\"雪☃\"}"u8.ToArray();

    void Because() => _signature = WebhookSigning.Sign("msg_é", 1700000000, _payload, _secret);

    /// <summary>
    /// Independent Node.js node:crypto HMAC-SHA256 fixture, secret bytes 0..31.
    /// UTF-8 frame hex: 6d73675fc3a92e313730303030303030302e7b2274657874223a22e99baae29883227d.
    /// </summary>
    [Fact] void should_sign_the_exact_utf8_identifier_and_payload() => _signature.ShouldEqual("v1,V+QZcBD6mND48NV9RUXH2vEHroP7s3mAZc11NwqXTdI=");
}
