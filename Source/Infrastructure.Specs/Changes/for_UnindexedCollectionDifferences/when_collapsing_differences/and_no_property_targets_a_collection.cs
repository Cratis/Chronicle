// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.when_collapsing_differences;

public class and_no_property_targets_a_collection : a_collapse_specification
{
    PropertyDifference _status;
    PropertyDifference _name;
    IReadOnlyList<PropertyDifference> _result;

    void Establish()
    {
        _status = new PropertyDifference("status", "pending", "approved");
        _name = new PropertyDifference("name", "Jane", "Jane Smith");
    }

    void Because()
    {
        var state = Expando(("status", "approved"), ("name", "Jane Smith"));
        _result = new[] { _status, _name }.Collapse(state, state);
    }

    [Fact] void should_return_every_difference() => _result.Count.ShouldEqual(2);

    [Fact] void should_leave_the_differences_untouched() =>
        (ReferenceEquals(_result[0], _status) && ReferenceEquals(_result[1], _name)).ShouldBeTrue();
}
