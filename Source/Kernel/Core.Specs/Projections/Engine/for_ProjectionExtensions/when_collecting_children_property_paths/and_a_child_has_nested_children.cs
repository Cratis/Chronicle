// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionExtensions.when_collecting_children_property_paths;

/// <summary>
/// A children collection may itself own one, and the exclusion has to reach it: the initial state a root
/// projection carries includes the grandchild's collection just as it includes the child's, and the same replay
/// race erases it just as readily. Walking only the immediate children collects the shallow path, which is enough
/// to make a single-level fixture pass while the deeper path is silently re-set to empty.
/// </summary>
public class and_a_child_has_nested_children : Specification
{
    IProjection _projection;
    PropertyPath[] _result;

    void Establish()
    {
        var grandchild = Substitute.For<IProjection>();
        grandchild.ChildrenPropertyPath.Returns(new PropertyPath("Members.Roles"));
        grandchild.ChildProjections.Returns([]);

        var child = Substitute.For<IProjection>();
        child.ChildrenPropertyPath.Returns(new PropertyPath("Members"));
        child.ChildProjections.Returns([grandchild]);

        _projection = Substitute.For<IProjection>();
        _projection.ChildrenPropertyPath.Returns(PropertyPath.Root);
        _projection.ChildProjections.Returns([child]);
    }

    void Because() => _result = _projection.GetChildrenPropertyPaths().ToArray();

    [Fact] void should_collect_the_child_path() => _result.ShouldContain(new PropertyPath("Members"));
    [Fact] void should_collect_the_nested_child_path() => _result.ShouldContain(new PropertyPath("Members.Roles"));
}
