// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.when_collapsing_differences;

public class and_a_collection_member_changed : a_collapse_specification
{
    object[] _original;
    object[] _changed;
    IReadOnlyList<PropertyDifference> _result;

    void Establish()
    {
        _original = Collection(Expando(("contactId", "c1"), ("name", "Jane")));
        _changed = Collection(Expando(("contactId", "c1"), ("name", "cipher")));
    }

    void Because() =>
        _result = new[] { new PropertyDifference("[contacts].name", "Jane", "cipher") }
            .Collapse(Expando(("contacts", _original)), Expando(("contacts", _changed)));

    [Fact] void should_produce_a_single_difference() => _result.Count.ShouldEqual(1);

    [Fact] void should_replace_the_whole_changed_collection() => ReferenceEquals(_result[0].Changed, _changed).ShouldBeTrue();

    [Fact] void should_carry_the_whole_original_collection() => ReferenceEquals(_result[0].Original, _original).ShouldBeTrue();
}
