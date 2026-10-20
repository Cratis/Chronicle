// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ClientDeliveryWait;

public class when_the_client_answers_in_time : Specification
{
    ObserverSubscriberResult _result;

    async Task Because()
    {
        var clock = new DeliveryClock();
        var context = new ObserverSubscriberContext(null).WithDeadlines(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(300), clock);
        _result = await ClientDeliveryWait.For(Task.FromResult(ObserverSubscriberResult.Ok(42)), result => result, context, clock, NullLogger.Instance, ObserverKey.NotSet, "partition");
    }

    [Fact] void should_preserve_the_result() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
