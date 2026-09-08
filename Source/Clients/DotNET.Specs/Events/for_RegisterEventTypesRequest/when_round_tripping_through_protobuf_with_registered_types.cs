// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventTypes;
using ProtoBuf;

namespace Cratis.Chronicle.Events.for_RegisterEventTypesRequest;

public class when_round_tripping_through_protobuf_with_registered_types : Specification
{
    RegisterEventTypesRequest _request;
    RegisterEventTypesRequest _result;

    void Establish() => _request = new()
    {
        EventStore = "EventStore",
        Types =
        [
            new EventTypeRegistration
            {
                Type = new Contracts.Events.EventType { Id = Guid.NewGuid().ToString(), Generation = 1 },
                Schema = "{}"
            }
        ]
    };

    void Because() => _result = Serializer.DeepClone(_request);

    [Fact] void should_keep_types_non_null() => _result.Types.ShouldNotBeNull();
    [Fact] void should_keep_the_registered_type() => _result.Types.Count().ShouldEqual(1);
    [Fact] void should_keep_the_type_id() => _result.Types.Single().Type.Id.ShouldEqual(_request.Types.Single().Type.Id);
}
