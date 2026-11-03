// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Registrations;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion.and_read_models_are_watched_in_two_namespaces.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_waiting_for_completion;

[Collection(ChronicleCollection.Name)]
public class and_read_models_are_watched_in_two_namespaces(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        readonly Guid _customerId = Guid.NewGuid();
        readonly Guid _markerId = Guid.NewGuid();
        readonly ConcurrentQueue<Guid> _defaultReceivedIds = new();
        readonly TaskCompletionSource _defaultMarkerReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<ReadModelChangeset<CustomerSnapshot>> _tenantReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
#pragma warning disable CA2213 // Disposable fields should be disposed
        IDisposable _defaultSubscription;
        IDisposable _tenantSubscription;
#pragma warning restore CA2213 // Disposable fields should be disposed

        public bool DefaultWatcherReceived;
        public CustomerSnapshot TenantWatcherModel;

        public override IEnumerable<Type> EventTypes => [typeof(CustomerNamed)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(CustomerSnapshot)];

        async Task Because()
        {
            // Keep the store name short so the PostgreSQL database prefix plus store and namespace
            // stays within its 63-byte identifier limit.
            var defaultStore = await ChronicleClient.GetEventStore("watch4492");
            await defaultStore.DiscoverAll();
            await defaultStore.RegisterAll();
            (await defaultStore.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();

            var tenantStore = await ChronicleClient.GetEventStore(defaultStore.Name, "acme");
            await tenantStore.DiscoverAll();
            await tenantStore.RegisterAll();
            (await tenantStore.WaitForRegistration(TimeSpan.FromSeconds(30))).IsSuccess.ShouldBeTrue();

            var defaultWatcher = defaultStore.ReadModels.GetWatcherFor<CustomerSnapshot>();
            var tenantWatcher = tenantStore.ReadModels.GetWatcherFor<CustomerSnapshot>();
            _defaultSubscription = defaultWatcher.Observable.Subscribe(changeset =>
            {
                if (changeset.ReadModel is null)
                {
                    return;
                }

                _defaultReceivedIds.Enqueue(changeset.ReadModel.Id);
                if (changeset.ReadModel.Id == _markerId)
                {
                    _defaultMarkerReceived.TrySetResult();
                }
            });
            _tenantSubscription = tenantWatcher.Observable.Subscribe(changeset => _tenantReceived.TrySetResult(changeset));

            // Both watches are registered with the kernel once Subscribed has signaled, so an event
            // appended after this point reaches every watch that is entitled to it.
            await defaultWatcher.Subscribed.WaitAsync(TimeSpan.FromSeconds(30));
            await tenantWatcher.Subscribed.WaitAsync(TimeSpan.FromSeconds(30));

            var appended = await tenantStore.EventLog.Append(_customerId, new CustomerNamed("Tenant customer"));
            appended.IsSuccess.ShouldBeTrue();

            TenantWatcherModel = (await _tenantReceived.Task.WaitAsync(TimeSpan.FromSeconds(30))).ReadModel;

            // The second namespace has been served. Append a marker in the default namespace and wait until its
            // watcher has it: a change leaked from the second namespace would have been dispatched to the
            // default watcher before the marker, so everything it received up to the marker is conclusive.
            (await defaultStore.EventLog.Append(_markerId, new CustomerNamed("Default marker"))).IsSuccess.ShouldBeTrue();
            await _defaultMarkerReceived.Task.WaitAsync(TimeSpan.FromSeconds(30));
            DefaultWatcherReceived = _defaultReceivedIds.Contains(_customerId);

            _defaultSubscription.Dispose();
            _tenantSubscription.Dispose();
        }
    }

    [Fact] void should_deliver_the_change_to_the_watcher_in_the_second_namespace() => Context.TenantWatcherModel.CustomerName.ShouldEqual("Tenant customer");
    [Fact] void should_not_deliver_the_change_to_the_watcher_in_the_default_namespace() => Context.DefaultWatcherReceived.ShouldBeFalse();
}
