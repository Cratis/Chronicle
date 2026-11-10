// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ChildrenSortValues.when_comparing;

public class dates : Specification
{
    static readonly DateTimeOffset _noonInOslo = new(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(1));
    static readonly DateTimeOffset _halfPastElevenUtc = new(2026, 1, 1, 11, 30, 0, TimeSpan.Zero);

    [Fact] void should_compare_date_time_offsets_as_utc_instants() => ChildrenSortValues.Compare(_noonInOslo, _halfPastElevenUtc).ShouldBeLessThan(0);
    [Fact] void should_compare_a_date_time_offset_and_a_utc_date_time_as_instants() => ChildrenSortValues.Compare(_halfPastElevenUtc, _halfPastElevenUtc.UtcDateTime).ShouldEqual(0);
    [Fact] void should_compare_at_millisecond_precision() => ChildrenSortValues.Compare(_halfPastElevenUtc, _halfPastElevenUtc.AddTicks(10)).ShouldEqual(0);
}
