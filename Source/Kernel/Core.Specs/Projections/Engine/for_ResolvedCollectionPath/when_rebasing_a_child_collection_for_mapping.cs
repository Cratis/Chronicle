// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ResolvedCollectionPath;

public class when_rebasing_a_child_collection_for_mapping : given.a_recursive_collection
{
    void Because() => _path = ResolvedCollectionPath.ForChild(_slices.ChildrenPropertyPath, new ArrayIndexers(
        [new("features", "id", "feature"), new("features.subFeatures.slices", "id", "slice")]));

    [Fact] void should_use_the_resolved_child_indexer() => _path.Path.ShouldEqual("features.subFeatures.slices");
}
