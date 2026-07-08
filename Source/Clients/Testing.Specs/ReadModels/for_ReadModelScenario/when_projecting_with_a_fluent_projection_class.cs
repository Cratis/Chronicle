// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that <see cref="ReadModelScenario{TReadModel}"/> can build and drive a standalone fluent
/// <see cref="Projections.IProjectionFor{TReadModel}"/> class — including a keyed child collection.
/// This path was previously dead-untested and threw an <see cref="System.ArgumentException"/> ("the number
/// of generic arguments provided doesn't equal the arity of the generic type definition") on first access to
/// <see cref="ReadModelScenario{TReadModel}.Instance"/>, because the definition builder was a generic type
/// nested inside the generic scenario and so had an arity of two.
/// </summary>
public class when_projecting_with_a_fluent_projection_class : Specification
{
    ReadModelScenario<FluentContactSheet> _scenario;
    SheetId _sheetId;
    ContactId _firstContact;
    ContactId _secondContact;

    void Establish()
    {
        _scenario = new ReadModelScenario<FluentContactSheet>();
        _sheetId = SheetId.New();
        _firstContact = new ContactId(Guid.NewGuid());
        _secondContact = new ContactId(Guid.NewGuid());
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_sheetId)
            .Events(
                new SheetStarted(2026),
                new ContactAssigned(_firstContact, "Ada"),
                new ContactAssigned(_secondContact, "Grace"));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_map_the_root_value() => _scenario.Instance!.Year.ShouldEqual(2026);
    [Fact] void should_have_two_contacts() => _scenario.Instance!.Contacts.Count().ShouldEqual(2);
    [Fact] void should_map_first_contact_id() => _scenario.Instance!.Contacts.First().ContactId.ShouldEqual(_firstContact);
    [Fact] void should_map_first_contact_name() => _scenario.Instance!.Contacts.First().Name.ShouldEqual("Ada");
    [Fact] void should_map_second_contact_id() => _scenario.Instance!.Contacts.Last().ContactId.ShouldEqual(_secondContact);
}
