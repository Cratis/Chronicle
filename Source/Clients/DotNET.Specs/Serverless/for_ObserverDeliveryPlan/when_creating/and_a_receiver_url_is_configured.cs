// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Serverless.for_ObserverDeliveryPlan.when_creating;

public class and_a_receiver_url_is_configured : Specification
{
    ChronicleOptions _options;
    ObserverDeliveryPlan _plan;
    Exception _error;

    void Establish() => _options = new ChronicleOptions { Serverless = { ReceiverUrl = new Uri("https://orders.example.com") } };

    void Because()
    {
        _plan = ObserverDeliveryPlan.Create(new ObserverDeliveries(_options), [("http-one", typeof(DeclaredHttp)), ("stream-one", typeof(object))]);
        _error = Catch.Exception(() => _plan.ThrowIfNotDeliverable(_options));
    }

    [Delivery(ObserverDelivery.Http)]
    class DeclaredHttp;

    [Fact] void should_split_http_observers() => _plan.Http.ShouldEqual("http-one");
    [Fact] void should_split_stream_observers() => _plan.Stream.ShouldEqual("stream-one");
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<HttpDeliveryNotAvailable>();
    [Fact] void should_name_the_observer() => _error.Message.Contains("http-one").ShouldBeTrue();
}
