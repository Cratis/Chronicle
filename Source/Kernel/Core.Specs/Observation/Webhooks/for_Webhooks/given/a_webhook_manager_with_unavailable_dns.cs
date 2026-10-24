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

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhooks.given;

public class a_webhook_manager_with_unavailable_dns : for_Observer.given.a_stored_webhook_with_unavailable_dns
{
    protected readonly TestKitSilo _managerSilo = new();
    protected Webhooks _manager;
    protected string _reminderName;

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

    protected static TickStatus Tick(TimeSpan period) => new(DateTime.UtcNow, period, DateTime.UtcNow);

    protected void ShouldSchedule(TimeSpan delay) => _managerSilo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), _reminderName, delay, delay), Times.Once);

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
