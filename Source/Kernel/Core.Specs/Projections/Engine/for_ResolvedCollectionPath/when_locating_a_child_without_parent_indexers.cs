// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ResolvedCollectionPath;

public class when_locating_a_child_without_parent_indexers : given.a_recursive_collection
{
    void Establish() => _parentKey = new Key("root", ArrayIndexers.NoIndexers);

    void Because() => _path = ResolvedCollectionPath.ForParent(_slices, _parentKey);

    [Fact] void should_keep_the_declared_collection_path() => _path.Path.ShouldEqual("features.slices");
}
