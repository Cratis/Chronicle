// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ChildrenSortValues.when_comparing;

public class guids : Specification
{
    /// <summary>
    /// With the standard representation the first stored byte is the most significant byte of the first group.
    /// </summary>
    static readonly Guid _firstGroupHigh = Guid.Parse("01000000-0000-0000-0000-000000000000");
    static readonly Guid _firstGroupLow = Guid.Parse("000000ff-0000-0000-0000-000000000000");
    static readonly Guid _lastByteHigh = Guid.Parse("00000000-0000-0000-0000-0000000000ff");

    [Fact] void should_compare_by_the_big_endian_bytes() => ChildrenSortValues.Compare(_firstGroupLow, _firstGroupHigh).ShouldBeLessThan(0);
    [Fact] void should_compare_the_last_bytes_last() => ChildrenSortValues.Compare(_lastByteHigh, _firstGroupLow).ShouldBeLessThan(0);
}
