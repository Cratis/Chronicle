// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookSigning;

public class when_computing_the_rfc4231_section_4_2_hmac : Specification
{
    byte[] _signature;

    void Because() => _signature = WebhookSigning.ComputeSignature("what do ya want for nothing?"u8, "Jefe"u8);

    /// <summary>
    /// RFC 4231 section 4.2, Test Case 2, HMAC-SHA-256. This tests the primitive, NOT webhook framing.
    /// See <see href="https://www.rfc-editor.org/rfc/rfc4231#section-4.2"/>.
    /// </summary>
    [Fact] void should_match_the_published_digest() => Convert.ToHexString(_signature).ShouldEqual("5BDCC146BF60754E6A042426089575C75A003F089D2739839DEC58B964EC3843");
}
