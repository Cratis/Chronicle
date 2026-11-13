// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager;

public class when_reading_visibility_before_registration : given.a_manager_with_shared_definitions
{
    ReadModelInstanceVisibility _visibility;

    void Establish() => _stored = _firstDefinition with
    {
        Children = new Dictionary<PropertyPath, ChildrenDefinition>
        {
            ["entries"] = new("id", new Dictionary<EventType, FromDefinition>(), new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>())
        }
    };

    async Task Because() => _visibility = await new ReadModelInstanceVisibilities(_manager).GetFor(_eventStore, _namespace, _firstReadModelDefinition with { ObserverIdentifier = _firstDefinition.Identifier.Value });

    [Fact] void should_include_uninitialized_child_only_roots() => _visibility.ShouldEqual(ReadModelInstanceVisibility.IncludeUninitialized);
    [Fact] void should_not_retain_a_compiled_graph() => _manager.CachedGraphCount.ShouldEqual(0);
    [Fact] void should_not_invoke_the_factory() => _ = _projectionFactory.DidNotReceive().Create(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
}
