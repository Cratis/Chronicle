// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.when_registering;

public class and_the_client_disconnects_while_subscribing : given.a_registration
{
    readonly CancellationTokenSource _cancellation = new();
    readonly TaskCompletionSource _subscribing = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _heldUntilRegistrationFinished;

    void Establish() => _client.SetDefinitionAndSubscribe(Arg.Any<ReactorDefinition>()).Returns(_ =>
    {
        _subscribing.TrySetResult();
        return _subscribed.Task;
    });

    async Task Because()
    {
        Connect(cancellationToken: _cancellation.Token);
        await _subscribing.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _cancellation.Cancel();
        _heldUntilRegistrationFinished = !_released.Task.IsCompleted;
        _subscribed.SetResult();
        await _released.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_keep_the_permit_until_in_flight_work_finishes() => _heldUntilRegistrationFinished.ShouldBeTrue();
    [Fact] void should_release_the_lease_after_disconnect() => _lease.Received(1).Dispose();
}
