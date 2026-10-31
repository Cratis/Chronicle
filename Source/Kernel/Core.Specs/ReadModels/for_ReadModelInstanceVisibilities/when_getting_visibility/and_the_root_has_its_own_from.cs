// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelInstanceVisibilities.when_getting_visibility;

public class and_the_root_has_its_own_from : given.a_visibility_resolver
{
    void Establish()
    {
        _projection.OperationTypes.Returns(new Dictionary<EventType, ProjectionOperationType>
        {
            [new EventType("RootCreated", EventTypeGeneration.First)] = ProjectionOperationType.From
        });
        ProjectionIsRegistered(_projection);
    }

    async Task Because() => _result = await _resolver.GetFor(EventStore, Namespace, DefinitionObservedBy(ReadModelObserverType.Projection));

    [Fact] void should_hide_uninitialized_roots() => _result.ShouldEqual(ReadModelInstanceVisibility.InitializedOnly);
}
