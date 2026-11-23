// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_RecursiveChildProjections;

public class when_resolving_an_event_owned_by_the_parent : given.two_projection_types
{
    IProjection _owner;

    void Because() => _owner = RecursiveChildProjections.OwnerOf(_parent, _parentEvent.Id);

    [Fact] void should_keep_the_parent_projection() => _owner.ShouldEqual(_parent);
}
