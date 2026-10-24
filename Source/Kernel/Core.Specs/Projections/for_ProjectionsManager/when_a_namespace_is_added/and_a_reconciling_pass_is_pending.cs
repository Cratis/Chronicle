// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_a_namespace_is_added;

public class and_a_reconciling_pass_is_pending : given.a_manager_with_a_quarantined_observer
{
    readonly TaskCompletionSource _reconcilingPassReachedObserver = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseReconcilingPass = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _establishedReads;

    void Establish()
    {
        // Only a reconciling pass asks whether the subscription is established: hold it there until the namespace is added.
        _managedObserver.IsSubscriptionEstablished().Returns(async _ =>
        {
            var established = await _observer.IsSubscriptionEstablished();
            if (Interlocked.Increment(ref _establishedReads) == 1)
            {
                _reconcilingPassReachedObserver.TrySetResult();
                await _releaseReconcilingPass.Task;
            }

            return established;
        });
    }

    async Task Because()
    {
        var reconciling = _manager.EnsureSubscribedIn(_observerKey.Namespace);
        await _reconcilingPassReachedObserver.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var fresh = _onNamespaceAdded(new NamespaceAdded(_observerKey.EventStore, _observerKey.Namespace));
        _releaseReconcilingPass.TrySetResult();
        await Task.WhenAll(reconciling, fresh).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] async Task should_end_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_subscribe_the_quarantined_observer() => await _managedObserver.Received(1).Subscribe<IProjectionObserverSubscriber>(ObserverType.Projection, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), reactivateRetired: false);
}
