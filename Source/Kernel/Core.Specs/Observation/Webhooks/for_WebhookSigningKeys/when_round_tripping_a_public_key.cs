// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningKeys;

public class when_round_tripping_a_public_key : Specification
{
    byte[] _key;
    string _encoded;
    byte[] _parsed;
    bool _result;

    void Establish() => _key = Enumerable.Range(0, 32).Select(_ => (byte)_).ToArray();

    void Because()
    {
        _encoded = WebhookSigningKeys.EncodePublicKey(_key);
        _result = WebhookSigningKeys.TryParsePublicKey(_encoded, out _parsed);
    }

    [Fact] void should_use_the_standard_prefix_and_base64() => _encoded.ShouldEqual($"whpk_{Convert.ToBase64String(_key)}");
    [Fact] void should_parse() => _result.ShouldBeTrue();
    [Fact] void should_preserve_the_key_bytes() => _parsed.ShouldEqual(_key);
}
