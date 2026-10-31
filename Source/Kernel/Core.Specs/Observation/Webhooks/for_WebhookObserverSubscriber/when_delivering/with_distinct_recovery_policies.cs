// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Security;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_distinct_recovery_policies : Specification
{
    WebhookObserverSubscriber _subscriber;
    PartitionDeferral _deferral;
    ObserverSubscriberResult _configuration;
    ObserverSubscriberResult _gone;
    ObserverSubscriberResult _deferred;

    async Task Establish()
    {
        var silo = new TestKitSilo();
        var target = new WebhookTarget("https://receiver.example", WebhookAuthorization.None, new Dictionary<string, string>());
        var definition = new WebhookDefinition("webhook", WebhookOwner.Client, "log", [], target);
        var state = Substitute.For<IStorage<WebhookDefinition>>();
        state.State = definition;
        silo.Options.StorageFactory = _ => state;
        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore("orders").Webhooks.Get("webhook").Returns(definition);
        var now = DateTimeOffset.UtcNow;
        _deferral = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, now, now.AddMinutes(2), EventSequenceNumber.First);
        var mediator = Substitute.For<IWebhookMediator>();
        mediator.Deliver((EventStoreName)"orders", Arg.Any<WebhookTarget>(), Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<string?>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookDeliveryResult(EventSequenceNumber.Unavailable, new Exception("403")) { FailurePolicy = WebhookDeliveryFailurePolicy.PauseObserver },
                new WebhookDeliveryResult(EventSequenceNumber.Unavailable, new Exception("410")) { FailurePolicy = WebhookDeliveryFailurePolicy.ParkPartition },
                new WebhookDeliveryResult(EventSequenceNumber.Unavailable, new Exception("429"), _deferral));
        silo.AddService(storage);
        silo.AddService(Substitute.For<IEncryption>());
        silo.AddService(Substitute.For<IOAuthClient>());
        silo.AddService(mediator);
        silo.AddService(Substitute.For<IEventGenerationRelease>());
        _subscriber = await silo.CreateGrainAsync<WebhookObserverSubscriber>(new ObserverSubscriberKey("webhook", "orders", "namespace", "log", "partition", string.Empty).ToString());
    }

    async Task Because()
    {
        _configuration = await _subscriber.OnNext("partition", [], new(null));
        _gone = await _subscriber.OnNext("partition", [], new(null));
        _deferred = await _subscriber.OnNext("partition", [], new(null));
    }

    [Fact] void should_preserve_the_configuration_failure_kind() => _configuration.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.PauseObserver);
    [Fact] void should_preserve_the_non_retryable_failure_kind() => _gone.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.ParkPartition);
    [Fact] void should_keep_backpressure_separate_from_failures() => _deferred.State.ShouldEqual(ObserverSubscriberState.Deferred);
    [Fact] void should_preserve_the_receiver_deadline() => _deferred.Deferral.ShouldEqual(_deferral);
}
