// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario.when_reading_read_models;

public class with_a_property_keyed_join : given.a_scenario
{
    JoinedProject _instance;

    async Task Establish()
    {
        var customerId = new CustomerId(Guid.NewGuid());
        await _scenario.Given.ForEventSource(new EventSourceId(customerId.Value)).Events(new CustomerRegistered("Ada"));
        await _scenario.Given.ForEventSource(_parentId).Events(new ProjectAssigned(customerId));
    }

    async Task Because() => _instance = await _scenario.ReadModels.GetInstanceById<JoinedProject>(_parentId.Value);

    [Fact] void should_join_the_customer_name_from_the_other_event_stream() => _instance.CustomerName.ShouldEqual("Ada");

    /// <summary>
    /// Identifies the customer stream joined into the project.
    /// </summary>
    /// <param name="Value">The customer identifier.</param>
    public record CustomerId(Guid Value) : EventSourceId<Guid>(Value);

    /// <summary>
    /// Records the customer name on the customer's own stream.
    /// </summary>
    /// <param name="Name">The customer name.</param>
    [EventType]
    public record CustomerRegistered(string Name);

    /// <summary>
    /// Associates the project with a customer on the project's own stream.
    /// </summary>
    /// <param name="CustomerId">The customer to join.</param>
    [EventType]
    public record ProjectAssigned(CustomerId CustomerId);

    /// <summary>
    /// Enriches a project by joining the customer event through its customer property.
    /// </summary>
    /// <param name="Id">The project identifier.</param>
    /// <param name="CustomerId">The join key.</param>
    /// <param name="CustomerName">The name from the customer's event.</param>
    [FromEvent<ProjectAssigned>]
    public record JoinedProject(
        [Key] Guid Id,
        [SetFrom<ProjectAssigned>] CustomerId CustomerId,
        [Join<CustomerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(CustomerRegistered.Name))] string CustomerName);
}
