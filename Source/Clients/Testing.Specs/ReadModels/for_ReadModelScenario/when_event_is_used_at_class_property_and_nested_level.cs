// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies the projection factory tolerates the same event type registered through multiple mechanisms on
/// one projection — class-level [FromEvent], property-level [SetFromContext], and a [Nested] child's [SetFrom].
/// Previously this raised 'An item with the same key has already been added. Key: ModuleOpened+1' from the
/// operation-types dictionary build.
/// </summary>
public class when_event_is_used_at_class_property_and_nested_level : Specification
{
    ReadModelScenario<ModuleSummary> _scenario;
    EventSourceId _moduleId;

    void Establish()
    {
        _scenario = new ReadModelScenario<ModuleSummary>();
        _moduleId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_moduleId)
            .Events(new ModuleOpened("Reporting", "Equinor ASA", "billing@equinor.com"));
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_map_module_name_from_event() => _scenario.Instance.Name.ShouldEqual("Reporting");
    [Fact] void should_populate_nested_customer() => _scenario.Instance.Customer.ShouldNotBeNull();
    [Fact] void should_map_nested_customer_name_from_same_event() => _scenario.Instance.Customer.Name.ShouldEqual("Equinor ASA");
    [Fact] void should_map_nested_customer_email_from_same_event() => _scenario.Instance.Customer.Email.ShouldEqual("billing@equinor.com");
    [Fact] void should_capture_opened_at_from_event_context() => _scenario.Instance.OpenedAt.ShouldNotBeNull();
}
