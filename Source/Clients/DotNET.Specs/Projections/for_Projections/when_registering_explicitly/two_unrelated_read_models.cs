// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class two_unrelated_read_models : given.projections_for_explicit_registration
{
    IProjectionHandler _inventory;
    IProjectionHandler _customer;

    async Task Because()
    {
        _inventory = await _projections.Register<Inventory>(projection => projection.From<ItemAdded>());
        _customer = await _projections.Register<Customer>(projection => projection.From<CustomerRegistered>());
    }

    [Fact] void should_know_the_first_read_model() => _projections.GetProjectionIdForModel<Inventory>().ShouldEqual(_inventory.Id);
    [Fact] void should_know_the_second_read_model() => _projections.GetProjectionIdForModel<Customer>().ShouldEqual(_customer.Id);
    [Fact] void should_hold_a_definition_for_each() => _projections.Definitions.Select(_ => _.Identifier).ShouldContainOnly([_inventory.Id.Value, _customer.Id.Value]);
}
