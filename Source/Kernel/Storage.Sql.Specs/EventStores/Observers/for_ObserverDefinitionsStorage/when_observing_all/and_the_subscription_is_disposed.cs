// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.for_ObserverDefinitionsStorage.when_observing_all;

public class and_the_subscription_is_disposed : given.an_observing_observer_definitions_storage
{
    readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _scopesAtDisposal;
    bool _anotherScopeCompleted;

    async Task Because()
    {
        await Subscribe();
        await WaitForCompletedScopes(3);
        _onScopeCompleted = () =>
        {
            _subscription.Dispose();
            _scopesAtDisposal = Volatile.Read(ref _completedScopes);
            while (_scopesReleased.Wait(0))
            {
            }
            _disposed.TrySetResult();
        };
        await _disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _anotherScopeCompleted = await _scopesReleased.WaitAsync(TimeSpan.FromMilliseconds(250));
    }

    [Fact] void should_stop_completing_poll_scopes() => _anotherScopeCompleted.ShouldBeFalse();
    [Fact] void should_not_keep_opening_poll_scopes() => Volatile.Read(ref _completedScopes).ShouldEqual(_scopesAtDisposal);
}
