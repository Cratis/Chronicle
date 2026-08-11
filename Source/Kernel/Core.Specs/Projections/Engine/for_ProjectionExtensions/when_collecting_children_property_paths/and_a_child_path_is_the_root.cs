// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionExtensions.when_collecting_children_property_paths;

/// <summary>
/// The collected paths are subtracted from the initial-state diff, so a root path among them subtracts the whole
/// model: every property would be read as owned by a children collection and nothing would be written at all. A
/// child sitting at the root - a nested projection rather than a collection - is exactly that case, and it has to
/// be dropped here rather than guarded at each of the three call sites that consume this.
/// </summary>
public class and_a_child_path_is_the_root : Specification
{
    IProjection _projection;
    PropertyPath[] _result;

    void Establish()
    {
        var collectionChild = Substitute.For<IProjection>();
        collectionChild.ChildrenPropertyPath.Returns(new PropertyPath("Members"));
        collectionChild.ChildProjections.Returns([]);

        var rootChild = Substitute.For<IProjection>();
        rootChild.ChildrenPropertyPath.Returns(PropertyPath.Root);
        rootChild.ChildProjections.Returns([]);

        _projection = Substitute.For<IProjection>();
        _projection.ChildrenPropertyPath.Returns(PropertyPath.Root);
        _projection.ChildProjections.Returns([collectionChild, rootChild]);
    }

    void Because() => _result = _projection.GetChildrenPropertyPaths().ToArray();

    [Fact] void should_collect_the_collection_path() => _result.ShouldContain(new PropertyPath("Members"));
    [Fact] void should_not_collect_the_root_path() => _result.ShouldNotContain(PropertyPath.Root);
}
