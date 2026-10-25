// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Receivers;

namespace Cratis.Chronicle.AspNetCore.Receivers.for_MapChronicleReceiver;

public class when_the_receiver_reports_a_failure : given.a_receiver_host
{
    private protected override DeliveryStatus OutcomeStatus => DeliveryStatus.Failed;
    Task Because() => Send();
    [Fact] void should_return_the_provisional_failure_status() => _response.StatusCode.ShouldEqual(HttpStatusCode.InternalServerError);
    [Fact] void should_not_expose_the_outcome_payload() => _responseBody.ShouldEqual(string.Empty);
}
