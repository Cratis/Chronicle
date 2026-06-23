// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.when_collapsing_differences;

public class and_the_original_state_is_null : a_collapse_specification
{
    object[] _changed;
    IReadOnlyList<PropertyDifference> _result;

    void Establish() => _changed = Collection(Expando(("contactId", "c1"), ("name", "cipher")));

    void Because() =>
        _result = new[] { new PropertyDifference("[contacts].name", null, "cipher") }
            .Collapse(null, Expando(("contacts", _changed)));

    [Fact] void should_produce_a_single_difference() => _result.Count.ShouldEqual(1);

    [Fact] void should_replace_the_whole_changed_collection() => ReferenceEquals(_result[0].Changed, _changed).ShouldBeTrue();

    [Fact] void should_carry_a_null_original_value() => _result[0].Original.ShouldBeNull();
}
