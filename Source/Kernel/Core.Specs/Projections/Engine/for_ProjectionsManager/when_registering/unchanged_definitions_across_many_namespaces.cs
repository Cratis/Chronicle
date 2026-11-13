// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_registering;

public class unchanged_definitions_across_many_namespaces : given.a_projections_manager
{
    async Task Because()
    {
        var namespaces = Enumerable.Range(0, 1000).Select(index => new EventStoreNamespaceName($"tenant-{index}")).ToArray();
        for (var reconnect = 0; reconnect < 10; reconnect++)
        {
            await _manager.Register(_eventStore, [_firstDefinition, _secondDefinition], [_firstReadModelDefinition, _secondReadModelDefinition], namespaces);
        }
    }

    [Fact] void should_validate_each_definition_only_once() => _ = _projectionFactory.Received(2).Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
    [Fact] void should_retain_only_the_validation_graphs() => _manager.CachedGraphCount.ShouldEqual(2);
}
