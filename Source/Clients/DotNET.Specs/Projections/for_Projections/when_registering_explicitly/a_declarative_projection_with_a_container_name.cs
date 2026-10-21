// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_declarative_projection_with_a_container_name : given.projections_for_explicit_registration
{
    async Task Because() => await _projections.Register<Inventory>(projection => projection.ContainerName("stock").From<ItemAdded>());

    [Fact] void should_know_the_container_name() => ((IKnowReadModelContainerNames)_projections).GetContainerNameOverride(typeof(Inventory)).ShouldEqual("stock");
    [Fact] void should_keep_the_read_model_identifier() => _projections.Definitions.Single().ReadModel.ShouldEqual(typeof(Inventory).FullName);
    [Fact] void should_not_know_a_container_name_for_other_read_models() => ((IKnowReadModelContainerNames)_projections).GetContainerNameOverride(typeof(Customer)).ShouldBeNull();
}
