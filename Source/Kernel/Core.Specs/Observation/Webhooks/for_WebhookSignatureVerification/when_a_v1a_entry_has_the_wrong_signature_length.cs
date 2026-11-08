// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_a_v1a_entry_has_the_wrong_signature_length : given.a_mixed_key_fixture
{
    string[] _headers;
    bool[] _alone;
    bool[] _withValidSignature;

    void Establish() => _headers = new[] { 0, 32, 63, 65 }.Select(length => $"v1a,{Convert.ToBase64String(new byte[length])}").ToArray();

    void Because()
    {
        _alone = _headers.Select(header => VerifyMixed(header, _newPublicKey)).ToArray();
        _withValidSignature = _headers.Select(header => VerifyMixed($"{header} {_newEd25519Signature}", _newPublicKey)).ToArray();
    }

    [Fact] void should_reject_alone() => _alone.Any(_ => _).ShouldBeFalse();
    [Fact] void should_ignore_when_accompanied_by_a_valid_entry() => _withValidSignature.All(_ => _).ShouldBeTrue();
}
