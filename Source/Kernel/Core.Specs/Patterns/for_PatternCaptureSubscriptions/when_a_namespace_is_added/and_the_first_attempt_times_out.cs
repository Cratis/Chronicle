// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patterns.for_PatternCaptureSubscriptions.when_a_namespace_is_added;

public class and_the_first_attempt_times_out : given.a_namespace_subscription
{
    int _attempts;

    void Establish() => _capture.Subscribe(_added.EventStore, _added.Namespace).Returns(_ =>
    {
        if (++_attempts == 1)
        {
            return Task.FromException(new TimeoutException());
        }

        _subscribed = true;
        return Task.CompletedTask;
    });

    Task Because() => _onNamespaceAdded(_added);

    [Fact] void should_recover_the_subscription() => _subscribed.ShouldBeTrue();
    [Fact] void should_stop_retrying_after_success() => _attempts.ShouldEqual(2);
}
