// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_HandleEvent.when_updating_a_root_with_a_resolved_join;

public class and_the_joined_property_is_null : given.a_root_join_with_initial_values
{
    void Establish()
    {
        StoreExistingOrder();
        _joinedEvent = _joinedEvent with { Content = new { name = (string?)null }.AsExpandoObject() };
    }

    Task Because() => ProjectOrder();

    [Fact] void should_persist_the_joined_null() => Order["customerName"].ShouldBeNull();
    [Fact] void should_preserve_unmapped_values() => Order["status"].ShouldEqual("existing");
}
