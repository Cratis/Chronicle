// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Options;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failed_partition_not_recovered;

public class and_the_retry_policy_changes_after_more_failures_than_its_new_limit : given.an_observer
{
    IOptionsMonitor<ChronicleOptions> _monitor;
    FailedPartitionId _episode;
    Exception? _error;

    async Task Establish()
    {
        _monitor = Substitute.For<IOptionsMonitor<ChronicleOptions>>();
        _monitor.CurrentValue.Returns(new ChronicleOptions { Observers = _observersConfig });
        var provider = new ConfigurationForObserverProvider(_monitor);
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(call => provider.GetFor(call.Arg<string>()));
        for (var attempt = 0; attempt < 4; attempt++)
        {
            await _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack");
        }
        _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(4);
        _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeFalse();
        _episode = _failedPartitionsState.Partitions.Single().Id;
        _monitor.CurrentValue.Returns(new ChronicleOptions
        {
            Observers = _observersConfig,
            ObserverRetryOverrides = new Dictionary<string, ObserverRetryOverrides>
            {
                [_observerKey.ToString()] = new() { MaxRetryAttempts = 1, BackoffDelay = 1, ExponentialBackoffDelayFactor = 1e8f, MaximumBackoffDelay = 600 }
            }
        });
        _silo.ReminderRegistry.Mock.Invocations.Clear();
    }

    async Task Because() => _error = await Catch.Exception(async () => await _observer.FailedPartitionNotRecovered("partition", _episode));

    [Fact] void should_not_throw_during_conversion() => _error.ShouldBeNull();
    [Fact] void should_rearm_once_at_the_cap() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.PartitionReminderName(_episode), TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(600)), Times.Once);
    [Fact] void should_register_only_one_additional_reminder() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Once);
    [Fact] void should_keep_the_historical_count() => _failedPartitionsState.Partitions.Single().AttemptsInCurrentBudget.ShouldEqual(4);
    [Fact] void should_keep_the_same_episode() => _failedPartitionsState.Partitions.Single().Id.ShouldEqual(_episode);
    [Fact] void should_keep_the_failure_unresolved() => _failedPartitionsState.Partitions.Single().IsResolved.ShouldBeFalse();
    [Fact] void should_not_add_quarantine() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeFalse();
}
