// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ResolvedCollectionPath;

public class when_locating_a_sibling_child_below_a_recursive_parent : given.a_recursive_collection
{
    void Because() => _path = ResolvedCollectionPath.ForParent(_slices, _parentKey);

    [Fact] void should_rebase_the_child_collection() => _path.Path.ShouldEqual("features.subFeatures.slices");
}
