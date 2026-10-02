// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.BroadcastChannel;
using Orleans.Hosting.for_ChronicleServerStartupTask.given;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_the_namespace_notification_exhausted_its_retries : given.an_event_sequence_with_pattern_capture
{
    bool _initialSubscriptionFailed;
    int _attempts;

    async Task Establish()
    {
        _patternCapture.Subscribe(EventStore, EventStoreNamespace).Returns(_ =>
        {
            if (++_attempts <= 3)
            {
                return Task.FromException(new TimeoutException());
            }

            _captureIsSubscribed = true;
            return Task.CompletedTask;
        });
        _captureIsSubscribed = false;
        Func<NamespaceAdded, Task> onNamespaceAdded = null!;
        var subscription = Substitute.For<IBroadcastChannelSubscription>();
        subscription.When(_ => _.Attach(Arg.Any<Func<NamespaceAdded, Task>>(), Arg.Any<Func<Exception, Task>>()))
            .Do(call => onNamespaceAdded = call.Arg<Func<NamespaceAdded, Task>>());
        var subscriptions = new PatternCaptureSubscriptions(_patternCapture, NullLogger<PatternCaptureSubscriptions>.Instance, new an_immediate_time_provider());
        await subscriptions.OnSubscribed(subscription);
        await onNamespaceAdded(new NamespaceAdded(EventStore, EventStoreNamespace));
        _initialSubscriptionFailed = !_captureIsSubscribed;
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_have_failed_the_initial_subscription() => _initialSubscriptionFailed.ShouldBeTrue();
    [Fact] void should_recover_without_another_notification() => _captureIsSubscribed.ShouldBeTrue();
}
