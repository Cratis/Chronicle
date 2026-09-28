// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_passive_declarative_projection : given.projections_for_explicit_registration
{
    async Task Because() => await _projections.Register<Inventory>(projection => projection.Passive().From<ItemAdded>());

    [Fact] void should_not_be_active() => _projections.Definitions.Single().IsActive.ShouldBeFalse();
    [Fact] void should_consider_the_read_model_passive() => ((IKnowPassiveProjections)_projections).IsPassive(typeof(Inventory)).ShouldBeTrue();
}
