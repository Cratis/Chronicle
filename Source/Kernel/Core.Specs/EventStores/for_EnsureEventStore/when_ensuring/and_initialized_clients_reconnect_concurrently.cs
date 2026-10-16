// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores.for_EnsureEventStore.when_ensuring;

public class and_initialized_clients_reconnect_concurrently : given.an_ensure_command
{
    const int FanIn = 4;
    readonly TaskCompletionSource _allRefreshesStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseRefreshes = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _refreshesStarted;

    void Establish()
    {
        _initialized = true;
        _eventTypes.DiscoverAndRegister(_name).Returns(_ =>
        {
            if (Interlocked.Increment(ref _refreshesStarted) == FanIn)
            {
                _allRefreshesStarted.TrySetResult();
            }

            return _releaseRefreshes.Task;
        });
    }

    async Task Because()
    {
        var ensures = Enumerable.Range(0, FanIn).Select(_ => Ensure()).ToArray();
        try
        {
            await _allRefreshesStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            _releaseRefreshes.TrySetResult();
        }

        await Task.WhenAll(ensures).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_start_every_refresh_without_waiting_for_another_to_finish() => _refreshesStarted.ShouldEqual(FanIn);
    [Fact] void should_bypass_the_cluster_serialized_initializer() => _initializerReference.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_register_projections() => _projections.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_refresh_reactors_for_every_connection() => await _reactors.Received(FanIn).DiscoverAndRegister(_name, EventStoreNamespaceName.Default);
    [Fact] async Task should_ensure_the_default_namespace_for_every_connection() => await _namespaces.Received(FanIn).EnsureDefault();
}
