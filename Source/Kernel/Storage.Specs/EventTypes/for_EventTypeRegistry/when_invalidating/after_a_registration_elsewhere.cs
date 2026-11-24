// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.EventTypes.for_EventTypeRegistry.when_invalidating;

public class after_a_registration_elsewhere : given.a_registry
{
    EventTypeSchema _before;
    EventTypeSchema _staleUntilAnnounced;
    EventTypeSchema _afterAnnouncement;
    EventTypeSchema _otherBefore;
    EventTypeSchema _otherAfter;
    long _revisionBefore;
    long _revisionAfter;

    async Task Establish()
    {
        _before = await _registry.GetFor(_eventTypeId, 2);
        _otherBefore = await _registry.GetFor(_otherEventTypeId, 1);
        _revisionBefore = (await _registry.GetSnapshot()).Revision;
    }

    async Task Because()
    {
        // Registered through another silo, straight into storage.
        await _storage.Register(new(_eventTypeId, 2), given.PlainEventTypesStorage.SchemaFor("second changed"));
        _staleUntilAnnounced = await _registry.GetFor(_eventTypeId, 2);

        _registry.Invalidate(_eventTypeId);
        _afterAnnouncement = await _registry.GetFor(_eventTypeId, 2);
        _otherAfter = await _registry.GetFor(_otherEventTypeId, 1);
        _revisionAfter = (await _registry.GetSnapshot()).Revision;
    }

    [Fact] void should_serve_the_loaded_schema_until_told() => _staleUntilAnnounced.Schema.Description.ShouldEqual("second");
    [Fact] void should_serve_the_new_schema_once_told() => _afterAnnouncement.Schema.Description.ShouldEqual("second changed");
    [Fact] void should_increase_the_revision() => _revisionAfter.ShouldEqual(_revisionBefore + 1);
    [Fact] void should_keep_the_entry_that_did_not_change_by_reference() => ReferenceEquals(_otherBefore, _otherAfter).ShouldBeTrue();
    [Fact] void should_replace_the_one_that_changed() => ReferenceEquals(_before, _afterAnnouncement).ShouldBeFalse();
}
