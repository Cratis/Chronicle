// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.given;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_UnindexedCollectionDifferences.when_collapsing_differences;

public class and_an_indexed_parent_has_a_nested_identity : a_collapse_specification
{
    object[] _originalChildren;
    object[] _changedChildren;
    IReadOnlyList<PropertyDifference> _result;

    void Establish()
    {
        _originalChildren = Collection(Expando(("topic", "First")));
        _changedChildren = Collection(Expando(("topic", "Updated")));
    }

    void Because()
    {
        var original = Expando(("parents", Collection(Expando(("identity", Expando(("id", "A"))), ("children", _originalChildren)))));
        var changed = Expando(("parents", Collection(Expando(("identity", Expando(("id", "A"))), ("children", _changedChildren)))));
        var difference = new PropertyDifference("[parents].[children].topic", "First", "Updated", new ArrayIndexers([new ArrayIndexer("[parents]", "identity.id", "A")]));
        _result = new[] { difference }.Collapse(original, changed);
    }

    [Fact] void should_replace_the_unindexed_children_collection() => _result.Single().PropertyPath.ShouldEqual(new PropertyPath("[parents].[children]"));
    [Fact] void should_retain_the_original_children() => _result.Single().Original.ShouldEqual(_originalChildren);
    [Fact] void should_retain_the_changed_children() => _result.Single().Changed.ShouldEqual(_changedChildren);
}
