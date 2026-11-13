// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager;

public class when_serving_many_namespaces : given.a_manager_with_shared_definitions
{
    readonly List<int> _retained = [];
    int _beforeVisibility;
    int _afterVisibility;

    async Task Because()
    {
        await _manager.Register(_eventStore, [_firstDefinition], [_firstReadModelDefinition], [_namespace]);
        var visibility = new ReadModelInstanceVisibilities(_manager);
        for (var pass = 0; pass < 2; pass++)
        {
            for (var tenant = 0; tenant < 1024; tenant++)
            {
                var @namespace = new EventStoreNamespaceName($"tenant-{tenant}");
                await _manager.AddNamespace(_eventStore, @namespace, [_firstReadModelDefinition]);
                await _manager.GetCurrent(_eventStore, @namespace, _firstDefinition.Identifier);
                if ((tenant + 1) % 256 == 0) _retained.Add(_manager.CachedGraphCount);
            }

            await _manager.Register(_eventStore, [_firstDefinition], [_firstReadModelDefinition], [_namespace]);
        }

        _beforeVisibility = _projectionFactory.ReceivedCalls().Count();
        var readModel = _firstReadModelDefinition with { ObserverIdentifier = _firstDefinition.Identifier.Value };
        for (var tenant = 0; tenant < 2048; tenant++)
        {
            await visibility.GetFor(_eventStore, $"uncompiled-{tenant}", readModel);
        }

        _afterVisibility = _projectionFactory.ReceivedCalls().Count();
    }

    [Fact] void should_plateau_at_the_global_capacity() => _retained.ShouldEqual(Enumerable.Repeat(ProjectionsManager.CacheCapacity, 8));
    [Fact] void should_not_compile_for_visibility_reads() => _afterVisibility.ShouldEqual(_beforeVisibility);
    [Fact] void should_not_recompile_on_reconnect_or_namespace_registration() => _ = _projectionFactory.Received(2049).Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
}
