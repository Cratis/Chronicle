// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Storage.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.given;

public class a_kernel_owned_projection_replay_handler : a_projection_replay_handler_with_projection
{
    void Establish()
    {
        var definitions = Substitute.For<IProjectionDefinitionsStorage>();
        definitions.Has(Arg.Any<ProjectionId>()).Returns(true);
        definitions.Get(Arg.Any<ProjectionId>()).Returns(new ProjectionDefinition(
            ProjectionOwner.Kernel,
            Concepts.EventSequences.EventSequenceId.Log,
            "TheObserver",
            "TheReadModelType",
            true,
            false,
            new(),
            new Dictionary<Concepts.Events.EventType, FromDefinition>(),
            new Dictionary<Concepts.Events.EventType, JoinDefinition>(),
            new Dictionary<Chronicle.Properties.PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<Chronicle.Properties.PropertyPath, string>(), false),
            new Dictionary<Concepts.Events.EventType, RemovedWithDefinition>(),
            new Dictionary<Concepts.Events.EventType, RemovedWithJoinDefinition>()));
        _eventStoreStorage.Projections.Returns(definitions);
    }
}
