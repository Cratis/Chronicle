// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_HandleEvent.when_creating_a_root_with_a_resolved_join;

public class and_the_joined_property_has_a_value : given.a_root_join_with_initial_values
{
    Task Because() => ProjectOrder();

    [Fact] void should_resolve_the_join() => _context.Changeset.Changes.OfType<ResolvedJoin>().Count().ShouldEqual(1);
    [Fact] void should_need_initial_state() => _context.NeedsInitialState.ShouldBeTrue();
    [Fact] void should_persist_the_joined_value() => Order["customerName"].ShouldEqual("Alice");
    [Fact] void should_persist_unmapped_initial_values() => Order["status"].ShouldEqual("pending");
    [Fact] void should_persist_the_join_key() => Order["customerId"].ShouldEqual("customer-c");
}
