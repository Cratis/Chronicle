// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.when_collapsing_differences;

public class and_several_members_of_a_collection_changed : a_collapse_specification
{
    object[] _changed;
    IReadOnlyList<PropertyDifference> _result;

    void Establish() => _changed = Collection(Expando(("contactId", "c1"), ("name", "cipher-name"), ("email", "cipher-email")));

    void Because() =>
        _result = new[]
        {
            new PropertyDifference("[contacts].name", "Jane", "cipher-name"),
            new PropertyDifference("[contacts].email", "jane@example.com", "cipher-email")
        }.Collapse(Expando(("contacts", Collection(Expando(("contactId", "c1"))))), Expando(("contacts", _changed)));

    [Fact] void should_collapse_to_a_single_collection_difference() => _result.Count.ShouldEqual(1);

    [Fact] void should_replace_the_whole_changed_collection() => ReferenceEquals(_result[0].Changed, _changed).ShouldBeTrue();
}
