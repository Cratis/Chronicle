// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections;

public class when_resolving_an_unrecognized_event : given.two_projection_types
{
    IProjection _owner;

    void Because() => _owner = RecursiveChildProjections.OwnerOf(_parent, "unrecognized-event");

    [Fact] void should_fall_back_to_the_parent_projection() => _owner.ShouldEqual(_parent);
}
