// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ResolvedCollectionPath;

public class when_mapping_a_root_collection : given.a_recursive_collection
{
    void Establish() => _slices.ChildrenPropertyPath.Returns(PropertyPath.Root);

    void Because() => _path = ResolvedCollectionPath.ForChild(_slices.ChildrenPropertyPath, _parentKey.ArrayIndexers);

    [Fact] void should_keep_the_root_path() => _path.ShouldEqual(PropertyPath.Root);
}
