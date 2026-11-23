// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ResolvedCollectionPath;

public class when_locating_a_recursive_child_below_a_recursive_parent : given.a_recursive_collection
{
    void Because() => _path = ResolvedCollectionPath.ForParent(_recursive, _parentKey);

    [Fact] void should_keep_the_collection_path_for_a_repeated_recursive_indexer() => _path.Path.ShouldEqual("features.subFeatures");
}
