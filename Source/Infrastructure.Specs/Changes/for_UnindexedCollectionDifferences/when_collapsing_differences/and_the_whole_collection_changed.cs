// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.when_collapsing_differences;

public class and_the_whole_collection_changed : a_collapse_specification
{
    PropertyDifference _difference;
    IReadOnlyList<PropertyDifference> _result;

    void Establish() =>
        _difference = new PropertyDifference(
            "[contacts]",
            Collection(Expando(("name", "Jane"))),
            Collection(Expando(("name", "cipher"))));

    void Because()
    {
        var state = Expando(("contacts", Collection(Expando(("name", "cipher")))));
        _result = new[] { _difference }.Collapse(state, state);
    }

    [Fact] void should_produce_a_single_difference() => _result.Count.ShouldEqual(1);

    [Fact] void should_leave_the_whole_collection_difference_untouched() => ReferenceEquals(_result[0], _difference).ShouldBeTrue();
}
