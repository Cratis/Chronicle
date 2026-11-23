// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildCollectionRemovals.when_removing_nested_children;

public class and_the_parent_key_is_composite : given.composite_children
{
    IList<object?> _parents;
    IList<object?> _siblingsChildren;
    bool _removed;

    void Establish()
    {
        _siblingsChildren = new List<object?> { Child("tenant", "target") };
        var siblingParent = Child("tenant", "other");
        var targetParent = Child("tenant", "target");
        ((IDictionary<string, object?>)siblingParent)["children"] = _siblingsChildren;
        ((IDictionary<string, object?>)targetParent)["children"] = _children;
        _parents = new List<object?> { siblingParent, targetParent };
    }

    void Because()
    {
        var removal = new ChildRemoval(new PropertyPath("[parents].[children]").Segments.ToArray(), "id", Key());
        _removed = ChildCollectionRemovals.RemoveNestedChild(_parents, removal, new ArrayIndexers([new ArrayIndexer("[parents]", "id", Key())]));
    }

    [Fact] void should_remove_the_matching_child() => _removed.ShouldBeTrue();
    [Fact] void should_keep_the_other_children_of_the_target_parent() => _children.ShouldEqual<object?>(_sameTenant, _sameTarget);
    [Fact] void should_not_remove_from_the_other_parent() => _siblingsChildren.Count.ShouldEqual(1);
}
