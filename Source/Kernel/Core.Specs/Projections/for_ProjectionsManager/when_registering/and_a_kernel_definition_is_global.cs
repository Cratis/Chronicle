// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_a_kernel_definition_is_global : given.a_projections_manager_grain
{
    ProjectionDefinition _remaining;
    ProjectionDefinition _global;

    void Establish()
    {
        _remaining = CreateDefinition("$system.supported", "read-model") with { Owner = ProjectionOwner.Kernel };
        _global = _remaining with { Identifier = "$system.unsupported.global", Scope = ProjectionScope.Global };
        _readModelDefinitions = [CreateReadModelDefinition(_remaining.ReadModel)];
    }

    async Task Because() => await _grain.Register([_global, _remaining]);

    [Fact] void should_register_the_other_kernel_projection() => _state.Projections.ShouldContainOnly(_remaining);
    [Fact] async Task should_not_forward_the_global_definition_to_the_engine() => await _projectionsServiceClient.DidNotReceive().Register(
        Arg.Any<EventStoreName>(), Arg.Is<IEnumerable<ProjectionDefinition>>(definitions => definitions.Any(definition => definition.Identifier == _global.Identifier)));
}
