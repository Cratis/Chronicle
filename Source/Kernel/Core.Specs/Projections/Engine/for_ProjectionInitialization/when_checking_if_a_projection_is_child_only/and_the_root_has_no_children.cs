// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionInitialization.when_checking_if_a_projection_is_child_only;

public class and_the_root_has_no_children : given.a_projection
{
    bool _result;

    void Establish() => _projection.ChildProjections.Returns([]);

    void Because() => _result = ProjectionInitialization.IsChildOnly(_projection);

    [Fact] void should_not_be_child_only() => _result.ShouldBeFalse();
}
