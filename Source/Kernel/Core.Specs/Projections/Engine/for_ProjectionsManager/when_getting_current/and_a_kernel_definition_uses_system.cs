// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_getting_current;

public class and_a_kernel_definition_uses_system : given.a_manager_with_shared_definitions
{
    readonly List<IProjection?> _results = [];

    async Task Because()
    {
        foreach (var sequence in new[] { "system", "SYSTEM", " SyStEm ", "\tSyStEm\r\n" })
        {
            _stored = _firstDefinition with { Owner = ProjectionOwner.Kernel, EventSequenceId = sequence };
            _results.Add(await _manager.GetCurrent(_eventStore, _namespace, _stored.Identifier));
        }
    }

    [Fact] void should_load_every_kernel_definition() => _results.All(_ => _ is not null).ShouldBeTrue();
    [Fact] void should_compile_every_variant() => _projectionFactory.Received(4).Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Is<ProjectionDefinition>(_ => _.Owner == ProjectionOwner.Kernel), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
}
