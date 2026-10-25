// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.AspNetCore.Receivers.for_MapChronicleReceiver;

public class when_posting_a_delivery : given.a_receiver_host
{
    Task Because() => Send();
    [Fact] void should_forward_the_observer_type() => _target!.ObserverType.ShouldEqual(ObserverType.Reactor);
    [Fact] void should_forward_the_observer_id() => _target!.ObserverId.ShouldEqual("my-reactor");
    [Fact] void should_forward_the_store() => _target!.EventStore.Value.ShouldEqual("store");
    [Fact] void should_forward_the_namespace() => _target!.Namespace.Value.ShouldEqual("tenant-a");
    [Fact] void should_forward_the_identical_body_bytes() => _body.SequenceEqual(CreateRequestBody()).ShouldBeTrue();
    [Fact] void should_forward_the_request_aborted_token() => _cancellationToken.CanBeCanceled.ShouldBeTrue();
    [Fact] void should_return_the_provisional_success_status() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_write_no_ack_body() => _responseBody.ShouldEqual(string.Empty);
}
