// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Namespaces.for_EnsureNamespace.when_ensuring;

public class and_subscriptions_are_pending : given.an_ensure_command
{
    readonly TaskCompletionSource _namespaceCreated = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _projectionSubscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _captureSubscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _projectionStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _captureStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _subscribedBeforeNamespaceCreated;

    void Establish()
    {
        _namespaces.Ensure(_namespace).Returns(_namespaceCreated.Task);
        _projections.EnsureSubscribedIn(_namespace).Returns(_ =>
        {
            _projectionStarted.TrySetResult();
            return _projectionSubscribed.Task;
        });
        _captures.EnsureSubscribedIn(_namespace).Returns(_ =>
        {
            _captureStarted.TrySetResult();
            return _captureSubscribed.Task;
        });
    }

    async Task Because()
    {
        var ensuring = _command.Handle(_grains);
        _subscribedBeforeNamespaceCreated = _projectionStarted.Task.IsCompleted || _captureStarted.Task.IsCompleted;
        _namespaceCreated.TrySetResult();
        await Task.WhenAll(_projectionStarted.Task, _captureStarted.Task).WaitAsync(TimeSpan.FromSeconds(5));
        _projectionSubscribed.TrySetResult();
        _captureSubscribed.TrySetResult();
        await ensuring.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_create_the_namespace_before_subscribing() => _subscribedBeforeNamespaceCreated.ShouldBeFalse();
}
