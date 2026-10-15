// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventTypes.for_EventTypesStorage;

public class when_registering_visibility_and_origin : given.an_event_types_storage
{
    static readonly EventTypeId _publicEvent = new("public-event");

    EventTypeSchema _legacy;
    EventTypeSchema _public;
    bool _firstRegistrationChanged;
    bool _sameRegistrationChanged;
    bool _visibilityChangeChanged;
    EventTypeSchema _afterUnspecified;
    EventTypeSchema _afterBecomingPrivate;

    async Task Because()
    {
        // The seeded row stands for one stored before visibility existed - it never had the columns set.
        _legacy = await _storage.GetFor(_eventTypeId, _firstGeneration);

        var schema = new JsonSchema();
        var type = new Concepts.Events.EventType(_publicEvent, _firstGeneration);
        _firstRegistrationChanged = await _storage.Register(type, schema, EventTypeOwner.Client, EventTypeSource.Code, EventTypeVisibility.Public, "owning-store");
        _sameRegistrationChanged = await _storage.Register(type, schema, EventTypeOwner.Client, EventTypeSource.Code, EventTypeVisibility.Public, "owning-store");
        _public = await _storage.GetFor(_publicEvent, _firstGeneration);

        await _storage.Register(type, schema, EventTypeOwner.Client, EventTypeSource.Code, EventTypeVisibility.Unspecified, "owning-store");
        _storage.Invalidate(_publicEvent);
        _afterUnspecified = await _storage.GetFor(_publicEvent, _firstGeneration);

        _visibilityChangeChanged = await _storage.Register(type, schema, EventTypeOwner.Client, EventTypeSource.Code, EventTypeVisibility.Private, "owning-store");
        _storage.Invalidate(_publicEvent);
        _afterBecomingPrivate = await _storage.GetFor(_publicEvent, _firstGeneration);
    }

    [Fact] void should_read_a_row_stored_before_visibility_existed_as_unspecified() => _legacy.Visibility.ShouldEqual(EventTypeVisibility.Unspecified);
    [Fact] void should_read_a_row_stored_before_visibility_existed_with_no_origin() => _legacy.Origin.ShouldEqual(string.Empty);
    [Fact] void should_report_the_first_registration_as_a_change() => _firstRegistrationChanged.ShouldBeTrue();
    [Fact] void should_report_the_same_registration_as_no_change() => _sameRegistrationChanged.ShouldBeFalse();
    [Fact] void should_persist_the_visibility() => _public.Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_persist_the_origin() => _public.Origin.ShouldEqual("owning-store");
    [Fact] void should_not_undo_the_visibility_when_a_client_sends_nothing() => _afterUnspecified.Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_report_a_visibility_change() => _visibilityChangeChanged.ShouldBeTrue();
    [Fact] void should_persist_the_changed_visibility() => _afterBecomingPrivate.Visibility.ShouldEqual(EventTypeVisibility.Private);
}
