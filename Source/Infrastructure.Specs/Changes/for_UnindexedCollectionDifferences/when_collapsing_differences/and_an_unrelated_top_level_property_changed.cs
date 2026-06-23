// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.when_collapsing_differences;

public class and_an_unrelated_top_level_property_changed : a_collapse_specification
{
    object[] _changed;
    IReadOnlyList<PropertyDifference> _result;

    void Establish() => _changed = Collection(Expando(("contactId", "c1"), ("name", "cipher")));

    void Because() =>
        _result = new[]
        {
            new PropertyDifference("[contacts].name", "Jane", "cipher"),
            new PropertyDifference("status", "pending", "approved")
        }.Collapse(Expando(("contacts", Collection(Expando(("contactId", "c1"))))), Expando(("contacts", _changed)));

    [Fact] void should_keep_both_the_collapsed_collection_and_the_top_level_difference() => _result.Count.ShouldEqual(2);

    [Fact] void should_leave_the_top_level_difference_untouched() =>
        _result.Single(_ => _.PropertyPath.Path == "status").Changed.ShouldEqual("approved");

    [Fact] void should_collapse_the_collection_difference() =>
        _result.Count(_ => ReferenceEquals(_.Changed, _changed)).ShouldEqual(1);
}
