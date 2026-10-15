// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_using_explicit_artifacts;

public class and_scalar_returning_helpers_do_not_change_delivery_routing : Specification
{
    ReactorScenario<Observer> _scenario;
    DeliveryLog _log;

    void Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<LocalEvent>(_ => _.ForEventStore("testing"))
            .RegisterEventType<ForeignEvent>(_ => _.ForEventStore("foreign"))
            .RegisterReactor<Observer>();
        _scenario = new(new Defaults(new InMemoryClientArtifactsProvider(), null, options));
        _log = new();
        _scenario.Services.AddSingleton(_log);
    }

    Task Because() => _scenario.Given.ForEventSourceId("local-partition").Events(new LocalEvent());
    void Destroy() => _scenario.Dispose();

    [Fact] void should_deliver_the_valid_local_event_on_the_event_log() => _log.Delivery.EventSequence.ShouldEqual(EventSequenceId.Log);

    public interface IContract;
    public record LocalEvent;
    public record ForeignEvent : IContract;
    public class DeliveryLog
    {
        public ReactorDelivery Delivery { get; set; }
    }
    public class Observer(DeliveryLog log)
    {
        public void Handle(LocalEvent @event, ReactorDelivery delivery) => log.Delivery = delivery;
        bool ShouldProcess(IContract @event) => false;
    }
}
