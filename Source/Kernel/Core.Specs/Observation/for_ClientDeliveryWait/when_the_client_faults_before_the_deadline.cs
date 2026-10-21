// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ClientDeliveryWait;

public class when_the_client_faults_before_the_deadline : Specification
{
    readonly TimeoutException _original = new("client error");
    Exception? _exception;

    async Task Because()
    {
        var clock = new DeliveryClock();
        var context = new ObserverSubscriberContext(null).WithDeadlines(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(300), clock);
        try
        {
            await new ClientDeliveryWait(clock, NullLogger.Instance, ObserverKey.NotSet, "partition").For(Task.FromException<ObserverSubscriberResult>(_original), result => result, context);
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_preserve_the_original_exception() => _exception.ShouldEqual(_original);
}
