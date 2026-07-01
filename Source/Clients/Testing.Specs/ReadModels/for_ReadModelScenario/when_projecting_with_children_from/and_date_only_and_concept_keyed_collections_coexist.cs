// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

/// <summary>
/// Verifies that a <see cref="DateOnly"/>-keyed child collection materializes even when a concept-keyed
/// child collection is present on the same read model — the shape a consumer reported, where the
/// concept-keyed collection populated but the <see cref="DateOnly"/>-keyed one appeared empty.
/// </summary>
public class and_date_only_and_concept_keyed_collections_coexist : Specification
{
    ReadModelScenario<MixedKeySheet> _scenario;
    SheetId _sheetId;
    ContactId _firstContact;
    ContactId _secondContact;

    void Establish()
    {
        _scenario = new ReadModelScenario<MixedKeySheet>();
        _sheetId = SheetId.New();
        _firstContact = new ContactId(Guid.NewGuid());
        _secondContact = new ContactId(Guid.NewGuid());
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_sheetId)
            .Events(
                new ConceptSheetStarted(2026),
                new ContactAssigned(_firstContact, "Ada"),
                new ConceptDayWorked(new DateOnly(2026, 6, 1), new WorkHours(7.5m)),
                new ContactAssigned(_secondContact, "Grace"),
                new ConceptDayWorked(new DateOnly(2026, 6, 2), new WorkHours(8m)));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_contacts() => _scenario.Instance!.Contacts.Count().ShouldEqual(2);
    [Fact] void should_have_two_days() => _scenario.Instance!.Days.Count().ShouldEqual(2);
    [Fact] void should_map_first_day() => _scenario.Instance!.Days.First().Day.ShouldEqual(new DateOnly(2026, 6, 1));
    [Fact] void should_map_first_contact() => _scenario.Instance!.Contacts.First().ContactId.ShouldEqual(_firstContact);
}
