// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookEd25519Signing;

public class when_generating_a_key : Specification
{
    byte[] _seed;
    byte[] _anotherSeed;

    void Because()
    {
        _seed = WebhookEd25519Signing.GenerateSeed();
        _anotherSeed = WebhookEd25519Signing.GenerateSeed();
    }

    [Fact] void should_generate_a_32_byte_seed() => _seed.Length.ShouldEqual(32);
    [Fact] void should_generate_distinct_seeds() => _seed.SequenceEqual(_anotherSeed).ShouldBeFalse();
    [Fact] void should_generate_a_usable_key() => WebhookEd25519Signing.VerifySignature("test"u8, WebhookEd25519Signing.ComputeSignature("test"u8, _seed), WebhookEd25519Signing.DerivePublicKey(_seed)).ShouldBeTrue();
}
