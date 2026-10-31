// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_EnsureNamespace.when_ensuring;

public class and_only_capture_subscription_is_pending : given.an_ensure_command
{
    readonly TaskCompletionSource _subscription = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _completedBeforeSubscription;

    void Establish() => _captures.EnsureSubscribedIn(_namespace).Returns(_subscription.Task);

    async Task Because()
    {
        var ensuring = _command.Handle(_grains);
        _completedBeforeSubscription = ensuring.IsCompleted;
        _subscription.TrySetResult();
        await ensuring.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_wait_for_the_capture_subscription() => _completedBeforeSubscription.ShouldBeFalse();
    [Fact] async Task should_subscribe_captures_in_the_requested_namespace() => await _captures.Received(1).EnsureSubscribedIn(_namespace);
}
