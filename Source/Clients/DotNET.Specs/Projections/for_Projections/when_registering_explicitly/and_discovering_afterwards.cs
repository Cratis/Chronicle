// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class and_discovering_afterwards : given.projections_for_explicit_registration
{
    IProjectionHandler _handler;

    async Task Establish()
    {
        _clientArtifacts.ModelBoundProjections.Returns([typeof(RegisteredCustomer)]);
        _handler = await _projections.Register<Inventory>(projection => projection.From<ItemAdded>());
    }

    async Task Because()
    {
        await _projections.Discover();
        await _projections.Register();
    }

    [Fact] void should_still_know_the_explicit_read_model() => _projections.HasFor<Inventory>().ShouldBeTrue();
    [Fact] void should_know_the_discovered_read_model() => _projections.HasFor<RegisteredCustomer>().ShouldBeTrue();
    [Fact] void should_send_both_in_the_full_registration() => _registrations.Single().Projections.Select(_ => _.Identifier).ShouldContainOnly([_handler.Id.Value, typeof(RegisteredCustomer).FullName]);
    [Fact] void should_claim_the_full_set() => _registrations.Single().FullSet.ShouldBeTrue();
}
