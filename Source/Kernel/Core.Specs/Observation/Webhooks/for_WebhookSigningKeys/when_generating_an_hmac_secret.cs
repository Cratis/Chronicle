// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningKeys;

public class when_generating_an_hmac_secret : Specification
{
    byte[] _secret;
    byte[] _anotherSecret;

    void Because()
    {
        _secret = WebhookSigningKeys.GenerateHmacSecret();
        _anotherSecret = WebhookSigningKeys.GenerateHmacSecret();
    }

    [Fact] void should_generate_32_bytes() => _secret.Length.ShouldEqual(32);
    [Fact] void should_generate_distinct_secrets() => _secret.SequenceEqual(_anotherSecret).ShouldBeFalse();
}
