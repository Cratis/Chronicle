// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionInitialization.when_checking_if_a_projection_is_child_only;

public class and_the_root_has_its_own_from : given.a_projection
{
    bool _result;

    void Establish() =>
        _projection.OperationTypes.Returns(new Dictionary<EventType, ProjectionOperationType>
        {
            [new EventType("RootCreated", EventTypeGeneration.First)] = ProjectionOperationType.From
        });

    void Because() => _result = ProjectionInitialization.IsChildOnly(_projection);

    [Fact] void should_not_be_child_only() => _result.ShouldBeFalse();
}
