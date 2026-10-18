// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

/// <summary>
/// Persistent cryptographic vector calculated independently with SHA-256 and RFC 5869 extract/expand.
/// </summary>
public class when_computing_the_persistent_v1_digest : given.an_append_intent
{
    void Establish() => Protect();
    void Because() => _result = Compute(_input, Use("name", ProtectionKind.Pii, _key));
    [Fact] void should_pin_the_plain_field_order_and_mask() => _result.Plain.ShouldEqual("3d4f7c8cacee92a86fa26be428e6550a54ad67033b9ab7ad2778e34504335632");
    [Fact] void should_pin_the_hkdf_and_hmac_encoding() => _result.ProtectedParts[0].Hash.ShouldEqual("52c17f207e7a093fbe1ffae5b0087a64a109f914cf136df33086f58cf3030d3e");
    [Fact] void should_pin_the_key_reference() => _result.ProtectedParts[0].KeyReference.ShouldEqual("039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81");
}
