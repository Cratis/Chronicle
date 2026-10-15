// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Chronicle.Storage.Observation.Delivery.for_DeliveryPages.when_mutating_source_items_after_creating;

public class and_both_pages_are_copied : for_DeliveryCommitValidator.given.a_valid_commit
{
    ParkedKey[] _parkedSource;
    SkipRange[] _skipSource;
    ParkedKeyPage _parkedPage;
    SkipRangePage _skipPage;

    void Establish()
    {
        _parkedSource = [_parked];
        _skipSource = [_skip];
        _parkedPage = new(ImmutableCollectionsMarshal.AsImmutableArray(_parkedSource), true);
        _skipPage = new(ImmutableCollectionsMarshal.AsImmutableArray(_skipSource), true);
    }

    void Because()
    {
        _parkedSource[0] = _parked with { Attempts = 99 };
        _skipSource[0] = _skip with { To = new(99) };
    }

    [Fact] void should_keep_the_parked_item() => _parkedPage.Items[0].ShouldEqual(_parked);
    [Fact] void should_keep_the_skip_item() => _skipPage.Items[0].ShouldEqual(_skip);
    [Fact] void should_compare_parked_pages_by_content() => _parkedPage.ShouldEqual(new ParkedKeyPage([_parked], true));
    [Fact] void should_compare_skip_pages_by_content() => _skipPage.ShouldEqual(new SkipRangePage([_skip], true));
    [Fact] void should_hash_parked_pages_by_content() => _parkedPage.GetHashCode().ShouldEqual(new ParkedKeyPage([_parked], true).GetHashCode());
    [Fact] void should_hash_skip_pages_by_content() => _skipPage.GetHashCode().ShouldEqual(new SkipRangePage([_skip], true).GetHashCode());
}
