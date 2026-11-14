// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ReactorObserverSubscriber;

public class when_a_batch_fails : given.a_subscriber
{
    readonly ILogger<ReactorObserverSubscriber> _logger = Substitute.For<ILogger<ReactorObserverSubscriber>>();
    ObserverSubscriberResult _expected;
    ObserverSubscriberResult _result;
    string[] _warnings;

    protected override ILogger<ReactorObserverSubscriber> CreateLogger()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        return _logger;
    }

    async Task Because()
    {
        AppendedEvent[] events = [AppendedEvent.EmptyWithEventSequenceNumber(10UL), AppendedEvent.EmptyWithEventSequenceNumber(11UL)];
        _expected = ObserverSubscriberResult.Failed(9UL, "private message");
        var delivery = _subscriber.OnNext("partition", events, _context);
        _completion.SetResult(_expected);
        _result = await delivery;
        _warnings = _logger.ReceivedCalls().Where(_ => _.GetMethodInfo().Name == "Log" && (LogLevel)_.GetArguments()[0]! == LogLevel.Warning).Select(_ => _.GetArguments()[2]!.ToString()!).ToArray();
    }

    [Fact] void should_return_the_original_result() => ReferenceEquals(_result, _expected).ShouldBeTrue();
    [Fact] void should_log_one_batch_warning() => _warnings.Length.ShouldEqual(1);
    [Fact] void should_log_the_batch_count() => _warnings.Single().Contains("batch of 2", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_log_the_sequence_range() => _warnings.Single().Contains("10-11", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_log_exception_details_in_the_warning() => _warnings.Single().Contains("private message", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_log_each_received_event() => _logger.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Log" && _.GetArguments()[2]!.ToString()!.Contains("Received event", StringComparison.Ordinal)).ShouldBeFalse();
}
