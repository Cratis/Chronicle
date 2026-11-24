// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ResolvedCollectionPath;

public class when_resolving_a_collection_without_a_parent : given.a_recursive_collection
{
    void Establish() => _slices.HasParent.Returns(false);

    void Because() => _path = ResolvedCollectionPath.ForParent(_slices, _parentKey);

    [Fact] void should_keep_the_declared_collection() => _path.Path.ShouldEqual("features.slices");
}
