// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_HandleEvent.when_updating_a_root_with_a_resolved_join;

public class and_the_joined_property_has_a_value : given.a_root_join_with_initial_values
{
    void Establish() => StoreExistingOrder();

    Task Because() => ProjectOrder();

    [Fact] void should_not_need_initial_state() => _context.NeedsInitialState.ShouldBeFalse();
    [Fact] void should_persist_the_joined_value() => Order["customerName"].ShouldEqual("Alice");
    [Fact] void should_preserve_unmapped_values() => Order["status"].ShouldEqual("existing");
    [Fact] void should_update_the_join_key() => Order["customerId"].ShouldEqual("customer-c");
}
