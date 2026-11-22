// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionsManager.when_registering;

public class and_a_non_kernel_definition_uses_system : given.a_projections_manager
{
    readonly List<ProjectionRegistrationError> _errors = [];

    async Task Because()
    {
        foreach (var owner in Enum.GetValues<ProjectionOwner>().Where(_ => _ != ProjectionOwner.Kernel))
        {
            foreach (var sequence in new[] { "system", "SYSTEM", " SyStEm ", "\tSyStEm\r\n" })
            {
                var result = await _manager.Register(_eventStore, [_firstDefinition with { Owner = owner, EventSequenceId = sequence }], [_firstReadModelDefinition], [_namespace]);
                result.TryGetError(out var error);
                _errors.Add(error);
            }
        }
    }

    [Fact] void should_report_every_unsupported_definition() => _errors.All(_ => _ is not null && _.Failures.ContainsKey(_firstDefinition.Identifier)).ShouldBeTrue();
    [Fact] void should_not_register_the_projection() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _).ShouldBeFalse();
    [Fact] void should_not_compile_the_projection() => _projectionFactory.DidNotReceive().Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>());
}
