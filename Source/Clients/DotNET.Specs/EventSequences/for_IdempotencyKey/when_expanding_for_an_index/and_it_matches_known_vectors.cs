// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_IdempotencyKey.when_expanding_for_an_index;

/// <summary>
/// Vectors computed with the kernel's PublicationIdentity encoding: SHA-256 of "len:part|" for (key, "batch", index), upper-case hex.
/// </summary>
public class and_it_matches_known_vectors : Specification
{
    IdempotencyKey _short;
    IdempotencyKey _long;

    void Establish()
    {
        _short = new("order-1");
        _long = new(new string('a', 512));
    }

    [Fact] void should_match_index_zero() => _short.ForIndex(0).Value.ShouldEqual("C34ED6CF1376D2BF3C3C9CE39004C7A569EC9AAF2246EE08FF08647AEEB907C8");
    [Fact] void should_match_index_one() => _short.ForIndex(1).Value.ShouldEqual("75577FF495A2341DAFDFC2C2E3D82E4E488A262047CA84DED53DBABF64C6B70E");
    [Fact] void should_match_index_twelve() => _short.ForIndex(12).Value.ShouldEqual("E74D4BD6882580B095FD72BF632A5057F54109915AFA85A1F5DDB729B7413AE3");
    [Fact] void should_match_the_longest_key() => _long.ForIndex(0).Value.ShouldEqual("A4E727F5ED0005EE1C8AF2C1675C75A6293BCD1370ECBADD8DD5635AE892B50D");
}
