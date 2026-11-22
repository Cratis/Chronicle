// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ResolvedCollectionPath;

public class when_locating_a_child_at_multiple_recursive_levels : given.a_recursive_collection
{
    void Establish() => _parentKey = new Key("root", new ArrayIndexers(
        [new("features", "id", "feature"), new("features.subFeatures", "id", "sub-feature"),
         new("features.subFeatures", "id", "nested-feature")]));

    void Because() => _path = ResolvedCollectionPath.ForParent(_slices, _parentKey);

    [Fact] void should_rebase_below_the_repeated_recursive_indexers() => _path.Path.ShouldEqual("features.subFeatures.slices");
}
