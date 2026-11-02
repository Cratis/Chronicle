// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Observation.for_ObserverDelivery;

public class when_reading_legacy_registration_payloads : Specification
{
    RegisterReactor _reactor;
    RegisterReducer _reducer;

    void Because()
    {
        // Field 4 contains a definition with only its identifier (field 1); no new metadata is present.
        byte[] payload = [0x22, 0x07, 0x0a, 0x05, 0x6f, 0x72, 0x64, 0x65, 0x72];
        using var reactorPayload = new MemoryStream(payload);
        using var reducerPayload = new MemoryStream(payload);
        _reactor = Serializer.Deserialize<RegisterReactor>(reactorPayload);
        _reducer = Serializer.Deserialize<RegisterReducer>(reducerPayload);
    }

    [Fact] void should_retain_the_reactor_identifier() => _reactor.Reactor.ReactorId.ShouldEqual("order");
    [Fact] void should_retain_the_reducer_identifier() => _reducer.Reducer.ReducerId.ShouldEqual("order");
    [Fact] void should_default_the_reactor_to_stream() => _reactor.Reactor.Delivery.ShouldEqual(ObserverDelivery.Stream);
    [Fact] void should_default_the_reducer_to_stream() => _reducer.Reducer.Delivery.ShouldEqual(ObserverDelivery.Stream);
    [Fact] void should_leave_the_reactor_http_target_unset() => _reactor.Reactor.HttpTarget.ShouldBeNull();
    [Fact] void should_leave_the_reducer_http_target_unset() => _reducer.Reducer.HttpTarget.ShouldBeNull();
    [Fact] void should_leave_the_reactor_program_unset() => _reactor.ProgramIdentifier.ShouldEqual(string.Empty);
    [Fact] void should_leave_the_reducer_program_unset() => _reducer.ProgramIdentifier.ShouldEqual(string.Empty);
}
