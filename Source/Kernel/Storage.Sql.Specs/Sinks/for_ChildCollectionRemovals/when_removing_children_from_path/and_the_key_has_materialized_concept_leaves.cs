// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildCollectionRemovals.when_removing_children_from_path;

public class and_the_key_has_materialized_concept_leaves : given.materialized_children
{
    ChildRemoval _removal;
    bool _removed;

    void Establish() => _removal = new(new PropertyPath("[children]").Segments.ToArray(), "id", _key);

    void Because() => _removed = ChildCollectionRemovals.RemoveChildrenFromPath(_document, 0, _removal);

    [Fact] void should_remove_the_target() => _removed.ShouldBeTrue();
    [Fact] void should_preserve_every_sibling() => _children.ShouldEqual<object?>(_subset, _ambiguous, _different);
}
