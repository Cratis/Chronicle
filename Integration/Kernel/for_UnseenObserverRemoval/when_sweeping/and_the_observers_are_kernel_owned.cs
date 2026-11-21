// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using context = Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping.and_the_observers_are_kernel_owned.context;

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.when_sweeping;

/// <summary>
/// Projections, webhooks, the kernel's own reactors (among them the ones that serve event store subscriptions and webhooks) are kernel observers, and are never removed, however old their recorded time is. A client
/// reactor that is unseen beside them is removed, which shows the sweep did run.
/// </summary>
/// <param name="context">The <see cref="context"/> the specification runs against.</param>
[Collection(given.ManualRemovalCollection.Name)]
public class and_the_observers_are_kernel_owned(context context) : Given<context>(context)
{
    public class context(given.ManualRemovalFixture fixture) : given.an_unseen_observer_kernel<given.ManualRemovalFixture>(fixture)
    {
        public string UnseenObserverId = string.Empty;
        public string[] KernelOwnedBefore = [];
        public string[] KernelOwnedAfter = [];
        public ObserverType[] TypesBefore = [];
        public int RemovedBySweep;
        public given.Footprint UnseenAfter = given.Footprint.None;

        async Task Because()
        {
            var projection = new given.UnseenProjection();
            var alpha = new given.UnseenAlphaReactor();
            var client = await StartClient(projection, alpha);
            UnseenObserverId = client.Reactor<given.UnseenAlphaReactor>().Id.Value;

            var source = await ChronicleClient.GetEventStore($"{StoreName}-source");
            await source.RegisterAll();
            await _store.Subscriptions.Subscribe("unseen-subscription", source.Name.Value, builder => builder.WithEventType<given.UnseenEvent>());
            await _store.Webhooks.Register("unseen-webhook", "https://localhost:9/unseen", builder => builder.WithEventType<given.UnseenEvent>());
            await _store.EventLog.Append("source", new given.UnseenEvent("first"));

            await WaitUntil("the projection and the webhook to be defined", async () =>
            {
                var defined = (await KernelOwned()).Select(_ => _.Identifier.Value).ToArray();
                return defined.Contains("unseen-webhook") && defined.Contains(typeof(given.UnseenProjection).FullName);
            });
            var owned = await KernelOwned();
            KernelOwnedBefore = [.. owned.Select(_ => _.Identifier.Value).Order()];
            TypesBefore = [.. owned.Select(_ => _.Type).Distinct()];

            await GoAwayLeavingTheObserverAsItIs(client, UnseenObserverId);
            foreach (var observer in owned)
            {
                await BackdateWhereThereIsAState(observer.Identifier.Value, TimeSpan.FromDays(365));
            }

            await WaitUntilUnseenForThePeriod(UnseenObserverId);
            RemovedBySweep = await Sweeper.Sweep();
            KernelOwnedAfter = [.. (await KernelOwned()).Select(_ => _.Identifier.Value).Order()];
            UnseenAfter = await FootprintOf(UnseenObserverId);
        }

        async Task<Cratis.Chronicle.Storage.Observation.ObserverDefinition[]> KernelOwned() =>
            [.. (await StoreStorage.Observers.GetAll()).Where(definition => definition.Owner != ObserverOwner.Client || definition.Type is not (ObserverType.Reactor or ObserverType.Reducer))];
    }

    [Fact] void should_have_a_projection_to_keep() => Context.KernelOwnedBefore.ShouldContain(typeof(given.UnseenProjection).FullName!);
    [Fact] void should_have_a_webhook_to_keep() => Context.KernelOwnedBefore.ShouldContain("unseen-webhook");
    [Fact] void should_have_the_kernel_reactors_of_subscriptions_and_webhooks_to_keep() => Context.KernelOwnedBefore.Count(id => id.StartsWith("$system.", StringComparison.Ordinal)).ShouldBeGreaterThanOrEqual(4);
    [Fact] void should_remove_only_the_client_reactor() => Context.RemovedBySweep.ShouldEqual(1);
    [Fact] void should_leave_nothing_of_the_client_reactor() => Context.UnseenAfter.ShouldEqual(given.Footprint.None);
    [Fact] void should_keep_every_kernel_owned_observer() => Context.KernelOwnedAfter.ShouldEqual(Context.KernelOwnedBefore);
}
