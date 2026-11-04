// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_Projections.when_discovering;

public class and_another_read_model_has_a_discovered_container_name : given.projections_for_explicit_registration
{
    IProjectionHandler _handler;

    void Establish()
    {
        _clientArtifacts.Projections.Returns([typeof(CustomerProjection)]);
        _artifactsActivator.ActivateNonDisposable<IProjectionFor<Customer>>(typeof(CustomerProjection)).Returns(new CustomerProjection());
        _projections = new Projections(_eventStore, _eventTypes, _clientArtifacts, _namingPolicy, _artifactsActivator, _jsonSerializerOptions, NullLogger<Projections>.Instance);
    }

    async Task Because()
    {
        await _projections.Discover();
        _handler = await _projections.Register<Inventory>(projection => projection.From<ItemAdded>());
    }

    [Fact] void should_use_the_naming_policy_for_the_explicit_read_model() => _handler.ContainerName.Value.ShouldEqual(_namingPolicy.GetReadModelName(typeof(Inventory)));
    [Fact] void should_report_the_effective_container_of_the_explicit_read_model() => _handler.ContainerName.Value.ShouldEqual(((IKnowReadModelContainerNames)_projections).GetContainerNameOverride(typeof(Inventory)) ?? _namingPolicy.GetReadModelName(typeof(Inventory)));
    [Fact] void should_preserve_the_other_read_models_discovered_override() => ((IKnowReadModelContainerNames)_projections).GetContainerNameOverride(typeof(Customer)).ShouldEqual("people");

    public class CustomerProjection : IProjectionFor<Customer>
    {
        public void Define(IProjectionBuilderFor<Customer> builder) => builder.ContainerName("people").From<CustomerRegistered>();
    }
}
