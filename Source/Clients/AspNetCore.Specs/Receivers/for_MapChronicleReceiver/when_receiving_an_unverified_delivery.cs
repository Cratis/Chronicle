// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.AspNetCore.Receivers.for_MapChronicleReceiver;

public class when_receiving_an_unverified_delivery : given.a_receiver_host
{
    protected override bool VerifiedDelivery => false;
    Task Because() => Send();
    [Fact] void should_fail_closed() => _response.StatusCode.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_not_dispatch() => _receiver.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_the_body() => _body.ShouldBeNull();
}
