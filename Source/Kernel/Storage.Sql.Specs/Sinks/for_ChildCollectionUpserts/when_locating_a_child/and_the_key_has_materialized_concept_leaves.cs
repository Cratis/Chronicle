// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildCollectionUpserts.when_locating_a_child;

public class and_the_key_has_materialized_concept_leaves : given.materialized_children
{
    ExpandoObject _result;

    void Because() => _result = ChildCollectionUpserts.LocateOrCreateChild(_children, _indexer);

    [Fact] void should_reuse_the_target() => _result.ShouldBeSame(_target);
    [Fact] void should_not_duplicate_the_target() => _children.Count.ShouldEqual(4);
}
