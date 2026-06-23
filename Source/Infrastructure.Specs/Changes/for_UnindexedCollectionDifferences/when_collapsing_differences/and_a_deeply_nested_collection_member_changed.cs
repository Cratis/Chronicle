// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.when_collapsing_differences;

public class and_a_deeply_nested_collection_member_changed : a_collapse_specification
{
    object[] _changedOrders;
    IReadOnlyList<PropertyDifference> _result;

    void Establish() =>
        _changedOrders = Collection(Expando(("lines", Collection(Expando(("price", "cipher"))))));

    void Because()
    {
        var changed = Expando(("orders", _changedOrders));
        _result = new[] { new PropertyDifference("[orders].[lines].price", "10", "cipher") }.Collapse(changed, changed);
    }

    [Fact] void should_produce_a_single_difference() => _result.Count.ShouldEqual(1);

    [Fact] void should_collapse_at_the_outermost_unindexed_collection() => ReferenceEquals(_result[0].Changed, _changedOrders).ShouldBeTrue();
}
