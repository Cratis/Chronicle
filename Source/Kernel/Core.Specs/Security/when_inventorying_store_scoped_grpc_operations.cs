// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

public class when_inventorying_store_scoped_grpc_operations : Specification
{
    string[] _operations;

    void Because() => _operations = given.store_scoped_grpc_operations.Contracts
        .SelectMany(given.store_scoped_grpc_inventory.For)
        .Distinct()
        .ToArray();

    [Fact] void should_inspect_a_nonempty_surface() => _operations.ShouldNotBeEmpty();
    [Fact] void should_list_every_store_scoped_operation() =>
        string.Join(", ", given.store_scoped_grpc_inventory.Unlisted(_operations, given.store_scoped_grpc_operations.All).Order()).ShouldEqual(string.Empty);
    [Fact] void should_not_list_an_operation_that_is_no_longer_store_scoped() =>
        string.Join(", ", given.store_scoped_grpc_operations.All.Keys.Where(id => !_operations.Contains(id)).Order()).ShouldEqual(string.Empty);
    [Fact] void should_discover_nested_reactor_registration() => _operations.ShouldContain("Cratis.Chronicle.Contracts.Observation.Reactors.IReactors.Observe");
    [Fact] void should_discover_nested_reducer_registration() => _operations.ShouldContain("Cratis.Chronicle.Contracts.Observation.Reducers.IReducers.Observe");
    [Fact] void should_include_subscription_target_selectors() => _operations.ShouldContain("Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.IEventStoreSubscriptions.Add");
}
