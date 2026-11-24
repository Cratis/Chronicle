// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildCollectionRemovals.when_removing_nested_children;

public class and_the_key_is_composite : given.composite_children
{
    bool _removed;

    void Because() => _removed = ChildCollectionRemovals.RemoveNestedChild(_children, new ChildRemoval(new PropertyPath("[children]").Segments.ToArray(), "id", Key()), ArrayIndexers.NoIndexers);

    [Fact] void should_remove_the_matching_child() => _removed.ShouldBeTrue();
    [Fact] void should_keep_both_siblings() => _children.ShouldEqual<object?>(_sameTenant, _sameTarget);
}
