// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigningKeys;

public class when_round_tripping_a_private_key : Specification
{
    byte[] _seed;
    string _encoded;
    byte[] _parsed;
    bool _result;

    void Establish() => _seed = Enumerable.Range(0, 32).Select(_ => (byte)_).ToArray();

    void Because()
    {
        _encoded = WebhookSigningKeys.EncodePrivateKey(_seed);
        _result = WebhookSigningKeys.TryParsePrivateKey(_encoded, out _parsed);
    }

    [Fact] void should_use_the_standard_prefix_and_base64() => _encoded.ShouldEqual($"whsk_{Convert.ToBase64String(_seed)}");
    [Fact] void should_parse() => _result.ShouldBeTrue();
    [Fact] void should_preserve_the_seed_bytes() => _parsed.ShouldEqual(_seed);
}
