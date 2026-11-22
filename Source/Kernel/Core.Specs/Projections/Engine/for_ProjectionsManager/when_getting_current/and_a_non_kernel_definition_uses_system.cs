// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_getting_current;

public class and_a_non_kernel_definition_uses_system : given.a_manager_with_shared_definitions
{
    readonly List<IProjection?> _results = [];
    readonly List<bool> _cached = [];

    async Task Because()
    {
        foreach (var owner in Enum.GetValues<ProjectionOwner>().Where(_ => _ != ProjectionOwner.Kernel))
        {
            foreach (var sequence in new[] { "system", "SYSTEM", " SyStEm ", "\tSyStEm\r\n" })
            {
                _stored = _firstDefinition;
                await _manager.GetCurrent(_eventStore, _namespace, _stored.Identifier);
                _stored = _firstDefinition with { Owner = owner, EventSequenceId = sequence };
                _results.Add(await _manager.GetCurrent(_eventStore, _namespace, _stored.Identifier));
                _cached.Add(_manager.TryGet(_eventStore, _namespace, _stored.Identifier, out _));
            }
        }
    }

    [Fact] void should_skip_every_non_kernel_definition_and_variant() => _results.All(_ => _ is null).ShouldBeTrue();
    [Fact] void should_remove_previously_cached_graphs() => _cached.All(_ => !_).ShouldBeTrue();
    [Fact] void should_not_compile_the_system_definitions() => _projectionFactory.DidNotReceive().Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Is<ProjectionDefinition>(_ => _.EventSequenceId.IsSystemSequence()), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
    [Fact] void should_leave_the_persisted_definitions_in_place() => _storage.GetEventStore(_eventStore).Projections.DidNotReceive().Delete(Arg.Any<ProjectionId>());
}
