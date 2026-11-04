// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigning;

public class when_signing_non_utf8_payload_bytes : given.a_signing_fixture
{
    string _signature;

    void Establish() => _payload = [0x00, 0xff, 0x80, 0x2e, 0x0d, 0x0a, 0xc3, 0x28];

    void Because() => _signature = WebhookSigning.Sign("msg_fixture", 1700000000, _payload, _secret);

    /// <summary>
    /// Independent Node.js node:crypto HMAC-SHA256 fixture, secret bytes 0..31.
    /// Frame hex: 6d73675f666978747572652e313730303030303030302e00ff802e0d0ac328.
    /// </summary>
    [Fact] void should_sign_without_decoding_or_reencoding_the_payload() => _signature.ShouldEqual("v1,ebPOU0PEDWuxaMhTSW9KCBWHNgVRlPO6SBF5unwSkx8=");
}
