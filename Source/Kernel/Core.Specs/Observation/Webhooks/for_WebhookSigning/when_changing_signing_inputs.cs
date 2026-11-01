// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigning;

public class when_changing_signing_inputs : given.a_signing_fixture
{
    string _original;
    string _changedTimestamp;
    string _changedId;
    string _changedSecret;
    string _changedPayload;

    void Establish() => _original = WebhookSigning.Sign("msg_fixture", 1700000000, _payload, _secret);

    void Because()
    {
        _changedTimestamp = WebhookSigning.Sign("msg_fixture", 1700000001, _payload, _secret);
        _changedId = WebhookSigning.Sign("msg_other", 1700000000, _payload, _secret);
        _changedSecret = WebhookSigning.Sign("msg_fixture", 1700000000, _payload, Enumerable.Repeat((byte)0x42, 32).ToArray());
        _changedPayload = WebhookSigning.Sign("msg_fixture", 1700000000, "{\"ok\": true}"u8, _secret);
    }

    [Fact] void should_authenticate_the_attempt_timestamp() => _changedTimestamp.ShouldNotEqual(_original);
    [Fact] void should_authenticate_the_message_identifier() => _changedId.ShouldNotEqual(_original);
    [Fact] void should_depend_on_the_secret() => _changedSecret.ShouldNotEqual(_original);
    [Fact] void should_authenticate_payload_whitespace() => _changedPayload.ShouldNotEqual(_original);
}
