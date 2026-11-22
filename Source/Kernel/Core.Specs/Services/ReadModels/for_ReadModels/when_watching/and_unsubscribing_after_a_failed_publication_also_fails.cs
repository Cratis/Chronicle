// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_watching;

public class and_unsubscribing_after_a_failed_publication_also_fails : given.a_watch_with_a_failed_initial_publication
{
    readonly Exception _unsubscribeFailure = new InvalidOperationException("Unsubscribe failed");

    void Establish() => _notifier.Unsubscribe(_subscriber).Returns(Task.FromException(_unsubscribeFailure));

    Task Because() => WatchUntilFaulted();

    [Fact] void should_attempt_to_remove_the_notifier_subscriber() => _notifier.Received(1).Unsubscribe(_subscriber);
    [Fact] void should_still_remove_the_registered_mediator_callback() => _changesetMediator.Received(1).Unsubscribe(_subscriptionId);
    [Fact] void should_have_registered_a_callback_before_failing() => _subscriptionId.ShouldNotEqual(Guid.Empty);
    [Fact] void should_report_the_unsubscribe_failure() => ReferenceEquals(_result, _unsubscribeFailure).ShouldBeTrue();
    [Fact] void should_not_acknowledge_the_watch() => _emitted.ShouldBeEmpty();
}
