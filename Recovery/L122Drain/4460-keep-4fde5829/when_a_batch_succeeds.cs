// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_a_batch_succeeds : given.a_subscriber
{
    readonly ILogger<ReactorObserverSubscriber> _logger = Substitute.For<ILogger<ReactorObserverSubscriber>>();
    ObserverSubscriberResult _result;

    protected override ILogger<ReactorObserverSubscriber> CreateLogger()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return _logger;
    }

    async Task Because()
    {
        AppendedEvent[] events = [AppendedEvent.EmptyWithEventSequenceNumber(10UL), AppendedEvent.EmptyWithEventSequenceNumber(11UL)];
        var delivery = _subscriber.OnNext("partition", events, _context);
        _completion.SetResult(ObserverSubscriberResult.Ok(11UL));
        _result = await delivery;
    }

    [Fact] void should_preserve_success() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_not_log_the_successful_batch() => _logger.ReceivedCalls().Where(_ => _.GetMethodInfo().Name == "Log").ShouldBeEmpty();
}
