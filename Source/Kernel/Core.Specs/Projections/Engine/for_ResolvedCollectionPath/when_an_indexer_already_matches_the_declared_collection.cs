// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ResolvedCollectionPath;

public class when_an_indexer_already_matches_the_declared_collection : given.a_recursive_collection
{
    void Because() => _path = ResolvedCollectionPath.ForChild(_recursive.ChildrenPropertyPath, _parentKey.ArrayIndexers);

    [Fact] void should_keep_the_declared_collection_path() => _path.Path.ShouldEqual("features.subFeatures");
}
