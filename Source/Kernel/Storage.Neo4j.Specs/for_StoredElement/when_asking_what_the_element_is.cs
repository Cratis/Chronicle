// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Neo4j.for_StoredElement;

public class when_asking_what_the_element_is : Specification
{
    StoredElement _written;
    StoredElement _bare;

    void Establish()
    {
        _written = new(new Dictionary<string, object>
        {
            [GraphBookkeeping.Watermark] = 5L,
            [GraphBookkeeping.Owner] = "owner",
            [GraphBookkeeping.Generation] = 3L
        });
        _bare = new(new Dictionary<string, object>());
    }

    [Fact] void should_read_the_watermark() => _written.Watermark.ShouldEqual(5L);
    [Fact] void should_read_the_owner() => _written.Owner.ShouldEqual("owner");
    [Fact] void should_read_the_generation() => _written.Generation.ShouldEqual(3L);
    [Fact] void should_have_no_watermark_when_only_an_edge_created_it() => _bare.Watermark.ShouldBeNull();
    [Fact] void should_have_no_owner_when_only_an_edge_created_it() => _bare.Owner.ShouldBeNull();
    [Fact] void should_have_generation_zero_when_only_an_edge_created_it() => _bare.Generation.ShouldEqual(0L);
    [Fact] void should_be_stale_for_its_owner_in_a_later_replay_generation() => _written.IsStale("owner", 4).ShouldBeTrue();
    [Fact] void should_not_be_stale_in_the_generation_it_was_written_in() => _written.IsStale("owner", 3).ShouldBeFalse();
    [Fact] void should_not_be_stale_when_the_owner_is_not_replaying() => _written.IsStale("owner", null).ShouldBeFalse();
    [Fact] void should_not_be_stale_for_another_owner() => _written.IsStale("other", 4).ShouldBeFalse();
    [Fact] void should_never_be_stale_when_it_has_no_owner() => _bare.IsStale("owner", 4).ShouldBeFalse();
    [Fact] void should_skip_a_write_at_the_watermark() => _written.IsAtOrBelowWatermark(5).ShouldBeTrue();
    [Fact] void should_skip_a_write_below_the_watermark() => _written.IsAtOrBelowWatermark(4).ShouldBeTrue();
    [Fact] void should_not_skip_a_write_above_the_watermark() => _written.IsAtOrBelowWatermark(6).ShouldBeFalse();
    [Fact] void should_not_skip_a_write_to_an_element_without_a_watermark() => _bare.IsAtOrBelowWatermark(1).ShouldBeFalse();
}
