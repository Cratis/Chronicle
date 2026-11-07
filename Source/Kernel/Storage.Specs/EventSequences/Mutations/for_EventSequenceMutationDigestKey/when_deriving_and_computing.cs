// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationDigestKey;

public class when_deriving_and_computing : Specification
{
    /// <summary>
    /// HKDF-SHA256(ikm = 0x00..0x1f, salt = empty, info = "cratis.chronicle.mutation-receipt-digest.v2" || int32be(11) || UTF8("event-store"), 32 bytes),
    /// then HMAC-SHA256 over "canonical". Computed independently with Node's crypto.
    /// </summary>
    const string Expected = "99deea40fb477a5303f87269bac4f9ca1ddc988715146753185c10157232badc";

    /// <summary>
    /// HMAC-SHA256 keyed with the raw secret, which the derived key must never equal.
    /// </summary>
    const string RawSecretHmac = "04beb982be178b425bdce1e2cf2994b9228470d7c2688d7f0924a7af37ae4186";

    EventSequenceMutationDigestKey _key;
    string _digest;

    void Establish() => _key = EventSequenceMutationDigestKey.DeriveFrom("event-store", Enumerable.Range(0, 32).Select(_ => (byte)_).ToArray());

    void Because() => _digest = Convert.ToHexStringLower(_key.Compute(Encoding.ASCII.GetBytes("canonical")));

    [Fact] void should_compute_the_pinned_hmac_over_the_derived_key() => _digest.ShouldEqual(Expected);
    [Fact] void should_not_key_with_the_raw_secret() => _digest.ShouldNotEqual(RawSecretHmac);
    [Fact] void should_not_expose_key_material_when_formatted() => _key.ToString().ShouldEqual("EventSequenceMutationDigestKey for 'event-store'");
}
