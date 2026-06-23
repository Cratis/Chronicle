// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.when_collapsing_differences;

public class and_a_collection_member_is_indexed : a_collapse_specification
{
    PropertyDifference _difference;
    IReadOnlyList<PropertyDifference> _result;

    void Establish() =>
        _difference = new PropertyDifference(
            new PropertyPath("[contacts].name"),
            "Jane",
            "Jane Smith",
            new ArrayIndexers([new ArrayIndexer(new PropertyPath("[contacts]"), new PropertyPath("contactId"), "c1")]));

    void Because()
    {
        var state = Expando(("contacts", Collection(Expando(("contactId", "c1"), ("name", "Jane")))));
        _result = new[] { _difference }.Collapse(state, state);
    }

    [Fact] void should_produce_a_single_difference() => _result.Count.ShouldEqual(1);

    [Fact] void should_leave_the_indexed_difference_untouched() => ReferenceEquals(_result[0], _difference).ShouldBeTrue();
}
