// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using ProtoBuf;

namespace Cratis.Chronicle.Services.Observation.for_ObserverDelivery;

public class when_round_tripping_registration_metadata : Specification
{
    RegisterReactor _reactor;
    RegisterReducer _reducer;

    void Because()
    {
        _reactor = Serializer.DeepClone(new RegisterReactor
        {
            ProgramIdentifier = "orders",
            Reactor = new() { Delivery = ObserverDelivery.Http, HttpTarget = new() { Url = "https://orders.example/reactor", Headers = new Dictionary<string, string> { ["X-Observer"] = "orders" } } }
        });
        _reducer = Serializer.DeepClone(new RegisterReducer
        {
            ProgramIdentifier = "orders",
            Reducer = new() { Delivery = ObserverDelivery.Http, HttpTarget = new() { Url = "https://orders.example/reducer", Headers = new Dictionary<string, string> { ["X-Observer"] = "orders" } } }
        });
    }

    [Fact] void should_round_trip_the_reactor_program() => _reactor.ProgramIdentifier.ShouldEqual("orders");
    [Fact] void should_round_trip_the_reducer_program() => _reducer.ProgramIdentifier.ShouldEqual("orders");
    [Fact] void should_round_trip_reactor_delivery() => _reactor.Reactor.Delivery.ShouldEqual(ObserverDelivery.Http);
    [Fact] void should_round_trip_reducer_delivery() => _reducer.Reducer.Delivery.ShouldEqual(ObserverDelivery.Http);
    [Fact] void should_round_trip_the_reactor_url() => _reactor.Reactor.HttpTarget.Url.ShouldEqual("https://orders.example/reactor");
    [Fact] void should_round_trip_the_reducer_url() => _reducer.Reducer.HttpTarget.Url.ShouldEqual("https://orders.example/reducer");
    [Fact] void should_round_trip_the_reactor_headers() => _reactor.Reactor.HttpTarget.Headers["X-Observer"].ShouldEqual("orders");
    [Fact] void should_round_trip_the_reducer_headers() => _reducer.Reducer.HttpTarget.Headers["X-Observer"].ShouldEqual("orders");
    [Fact] void should_reserve_the_future_http_target_fields() => ((ReservedProtoFieldsAttribute)Attribute.GetCustomAttribute(typeof(HttpTarget), typeof(ReservedProtoFieldsAttribute))!).FieldNumbers.ShouldEqual([3, 4, 5, 6, 7, 8, 9]);
}
