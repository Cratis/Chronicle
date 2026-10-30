// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionInitialization.given;

public class a_projection : Specification
{
    protected IProjection _projection;

    void Establish()
    {
        _projection = Substitute.For<IProjection>();
        _projection.HasParent.Returns(false);
        _projection.SubscribesToAllEvents.Returns(false);
        _projection.ChildProjections.Returns([Substitute.For<IProjection>()]);
        _projection.OperationTypes.Returns(new Dictionary<EventType, ProjectionOperationType>
        {
            [new EventType("RootRemoved", EventTypeGeneration.First)] = ProjectionOperationType.Remove
        });
    }
}
