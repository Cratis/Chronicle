// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookEd25519Signing;

public class when_verifying_with_an_invalid_public_point : given.an_ed25519_fixture
{
    bool _result;

    void Because() => _result = WebhookEd25519Signing.VerifySignature([], _test1Signature, Enumerable.Repeat((byte)0xff, 32).ToArray());

    [Fact] void should_reject_without_throwing() => _result.ShouldBeFalse();
}
