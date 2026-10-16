// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_ensuring;

public class and_clients_connect_concurrently : given.an_ensure_command
{
    void Establish() => _storage.HasEventStore(_name).Returns(true);

    async Task Because()
    {
        var registrationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRegistration = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _projections.DiscoverAndRegister(_name).Returns(_ =>
        {
            registrationStarted.TrySetResult();
            return releaseRegistration.Task;
        });
        var first = Ensure();
        await registrationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = Ensure();
        releaseRegistration.SetResult();
        await Task.WhenAll(first, second);
    }

    [Fact] async Task should_have_both_clients_reconcile_the_projection_set() => await _projections.Received(2).DiscoverAndRegister(_name);
    [Fact] void should_not_duplicate_store_added_notifications() => _systemSequence.ReceivedCalls().ShouldBeEmpty();
}
