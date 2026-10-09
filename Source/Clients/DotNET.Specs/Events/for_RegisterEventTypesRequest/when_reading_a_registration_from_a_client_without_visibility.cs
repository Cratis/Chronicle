// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Events.for_RegisterEventTypesRequest;

public class when_reading_a_registration_from_a_client_without_visibility : Specification
{
    [ProtoContract]
    class RegistrationBeforeVisibility
    {
        [ProtoMember(1)]
        public Contracts.Events.EventType Type { get; set; } = new();

        [ProtoMember(2)]
        public string Schema { get; set; } = string.Empty;

        [ProtoMember(5)]
        public string EventStore { get; set; } = string.Empty;
    }

    EventTypeRegistration _result;
    RegistrationBeforeVisibility _readByOldKernel;

    void Because()
    {
        var oldClientPayload = new MemoryStream();
        Serializer.Serialize(oldClientPayload, new RegistrationBeforeVisibility
        {
            Type = new Contracts.Events.EventType { Id = "an-event", Generation = 1 },
            Schema = "{}",
            EventStore = "owning-store"
        });
        oldClientPayload.Position = 0;
        _result = Serializer.Deserialize<EventTypeRegistration>(oldClientPayload);

        var newClientPayload = new MemoryStream();
        Serializer.Serialize(newClientPayload, new EventTypeRegistration
        {
            Type = new Contracts.Events.EventType { Id = "an-event", Generation = 1 },
            Schema = "{}",
            Visibility = EventTypeVisibility.Private
        });
        newClientPayload.Position = 0;
        _readByOldKernel = Serializer.Deserialize<RegistrationBeforeVisibility>(newClientPayload);
    }

    [Fact] void should_read_the_visibility_as_unspecified() => _result.Visibility.ShouldEqual(EventTypeVisibility.Unspecified);
    [Fact] void should_still_read_the_rest_of_the_registration() => _result.EventStore.ShouldEqual("owning-store");
    [Fact] void should_let_an_older_kernel_ignore_the_visibility_of_a_newer_client() => _readByOldKernel.Type.Id.ShouldEqual("an-event");
}
