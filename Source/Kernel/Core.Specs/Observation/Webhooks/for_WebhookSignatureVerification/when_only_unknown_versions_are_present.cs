// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSignatureVerification;

public class when_only_unknown_versions_are_present : given.a_verification_fixture
{
    bool _result;

    void Because() => _result = Verify("v1a,AAAA v2,AAAA", "key_new", _newSecret);

    [Fact] void should_reject() => _result.ShouldBeFalse();
}
