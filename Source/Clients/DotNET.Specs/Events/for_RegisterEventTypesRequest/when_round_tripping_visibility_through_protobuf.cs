// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using ProtoBuf;

namespace Cratis.Chronicle.Events.for_RegisterEventTypesRequest;

public class when_round_tripping_visibility_through_protobuf : Specification
{
    EventTypeRegistration _registration;
    EventTypeRegistration _result;

    void Establish() => _registration = new()
    {
        Type = new Contracts.Events.EventType { Id = "an-event", Generation = 1 },
        Schema = "{}",
        EventStore = "owning-store",
        Visibility = EventTypeVisibility.Public
    };

    void Because() => _result = Serializer.DeepClone(_registration);

    [Fact] void should_keep_the_visibility() => _result.Visibility.ShouldEqual(EventTypeVisibility.Public);
    [Fact] void should_keep_the_origin() => _result.EventStore.ShouldEqual("owning-store");
}
