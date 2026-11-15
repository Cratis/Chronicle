// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.given;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_activating;

public class and_refreshing_a_subscription_fails : a_manager_with_a_quarantined_subscription
{
    Exception? _error;

    void Establish() => _observer.IsSubscribed().Returns<bool>(_ => throw new InvalidOperationException("db already exists with different case"));

    async Task Because() => _error = await Catch.Exception(() => _manager.OnActivateAsync(CancellationToken.None));

    [Fact] void should_not_fail_the_activation() => _error.ShouldBeNull();
    [Fact] void should_still_schedule_the_reminder_that_retries() => ShouldScheduleReminder(2);
    [Fact] void should_log_the_failure_once() => _logger.Failures.Count.ShouldEqual(1);
    [Fact] void should_log_the_failure_with_the_namespace_it_failed_in() => _logger.Failures[0].Exception!.Message.ShouldContain(EventStoreNamespaceName.Default.Value);
    [Fact] void should_log_the_failure_with_its_cause() => _logger.Failures[0].Exception!.InnerException.ShouldBeOfExactType<InvalidOperationException>();
}
