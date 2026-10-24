// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhooks;

public class when_dns_recovers_after_activation : for_Observer.given.a_stored_webhook_with_unavailable_dns
{
    readonly TestKitSilo _managerSilo = new();
    Webhooks _manager;
    bool _initiallySubscribed;
    bool _eventuallySubscribed;
    string _reminderName;

    void Establish()
    {
        var definition = new WebhookDefinition(_observerId.Value, WebhookOwner.Kernel, EventSequenceId.Log, ["Event1"], new("https://receiver.example/hook", WebhookAuthorization.None, new Dictionary<string, string>()));
        _reminderName = $"webhook-subscribe:{definition.Identifier}";
        _managerSilo.AddProbe(_ => Substitute.For<IWebhook>());
        _managerSilo.AddProbe(_ => ObserverProxy(definition));
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);
        _managerSilo.AddProbe(_ => namespaces);
        _managerSilo.AddService(Substitute.For<ILocalSiloDetails>());
        _managerSilo.AddService(NullLogger<Webhooks>.Instance);
        _managerSilo.StorageManager.GetStorage<WebhooksState>(typeof(Webhooks).FullName).State = new() { Webhooks = [definition] };
    }

    async Task Because()
    {
        _manager = await _managerSilo.CreateGrainAsync<Webhooks>("orders");
        _initiallySubscribed = await _observer.IsSubscribed();
        await _manager.ReceiveReminder(_reminderName, Tick(TimeSpan.FromMinutes(1)));
        await _manager.ReceiveReminder(_reminderName, Tick(TimeSpan.FromMinutes(30)));
        _dnsAvailable = true;
        await _manager.ReceiveReminder(_reminderName, Tick(TimeSpan.FromMinutes(2)));
        _eventuallySubscribed = await _observer.IsSubscriptionEstablished();
    }

    [Fact] void should_activate_without_installing_an_unvalidated_subscription() => _initiallySubscribed.ShouldBeFalse();
    [Fact] void should_schedule_an_automatic_retry() => ShouldSchedule(TimeSpan.FromMinutes(1));
    [Fact] void should_back_off_when_dns_is_still_down() => ShouldSchedule(TimeSpan.FromMinutes(2));
    [Fact] void should_cap_the_backoff() => ShouldSchedule(TimeSpan.FromMinutes(10));
    [Fact] void should_subscribe_when_the_scheduled_retry_observes_dns_recovery() => _eventuallySubscribed.ShouldBeTrue();
    [Fact] async Task should_remove_the_retry_reminder_after_recovery() => (await _manager.GetReminder(_reminderName)).ShouldBeNull();
    [Fact] void should_never_quarantine_the_observer() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);

    static TickStatus Tick(TimeSpan period) => new(DateTime.UtcNow, period, DateTime.UtcNow);

    void ShouldSchedule(TimeSpan delay) => _managerSilo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), _reminderName, delay, delay), Times.Once);

    IObserver ObserverProxy(WebhookDefinition definition)
    {
        var proxy = Substitute.For<IObserver>();
        proxy.IsSubscribed().Returns(_ => _observer.IsSubscribed());
        proxy.NeedsSubscriptionRecovery(Arg.Any<IEnumerable<EventType>>()).Returns(_ => _observer.NeedsSubscriptionRecovery(definition.EventTypes));
        proxy.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), false, true)
            .Returns(_ => _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, definition.EventTypes, SiloAddress.Zero, reactivateRetired: false, automatic: true));
        return proxy;
    }
}
