// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigning;

public class when_signing_an_ascii_payload : given.a_signing_fixture
{
    byte[] _originalSecret;
    byte[] _originalPayload;
    string _signature;

    void Establish()
    {
        _originalSecret = _secret.ToArray();
        _originalPayload = _payload.ToArray();
    }

    void Because() => _signature = WebhookSigning.Sign("msg_fixture", 1700000000, _payload, _secret);

    /// <summary>
    /// Independent fixture: Node.js node:crypto createHmac('sha256', bytes 0..31).
    /// update(Buffer.concat([Buffer.from('msg_fixture.1700000000.', 'utf8'), Buffer.from('{"ok":true}')])).digest('base64').
    /// Framing: https://github.com/standard-webhooks/standard-webhooks/blob/main/spec/standard-webhooks.md (v1.0.0).
    /// </summary>
    [Fact] void should_match_the_framed_signature() => _signature.ShouldEqual("v1,39qeYL108/h2ZmkwgyNxazc/d5zHFOHtYcIZ6JO+bOs=");
    [Fact] void should_not_mutate_the_secret() => _secret.ShouldEqual(_originalSecret);
    [Fact] void should_not_mutate_the_payload() => _payload.ShouldEqual(_originalPayload);
}
