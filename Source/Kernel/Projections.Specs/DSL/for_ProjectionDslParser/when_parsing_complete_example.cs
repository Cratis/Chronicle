// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.DSL;

namespace Cratis.Chronicle.Projections.for_ProjectionDslParser;

public class when_parsing_complete_example : Specification
{
    const string Dsl = @"Users
| key=UserRegistered.userId
| name=UserRegistered.name
| currentOrderCount increment by OrderStarted
| currentOrderCount decrement by OrderCompleted
| orders=[
|    identified by orderId
|    key=OrderPlaced.orderId
|    total=OrderPlaced.total
| ]";

    ProjectionDefinition _result;

    void Because() => _result = ProjectionDsl.Parse(
        Dsl,
        new ProjectionId("test-projection"),
        ProjectionOwner.Client,
        EventSequenceId.Log);

    [Fact] void should_set_read_model_name() => _result.ReadModel.Value.ShouldEqual("Users");
    [Fact] void should_have_event_types_in_from() => _result.From.Count.ShouldBeGreaterThan(0);
    [Fact] void should_have_children() => _result.Children.Count.ShouldEqual(1);
    [Fact] void should_have_orders_children() => _result.Children.ContainsKey(new("orders")).ShouldBeTrue();
}
