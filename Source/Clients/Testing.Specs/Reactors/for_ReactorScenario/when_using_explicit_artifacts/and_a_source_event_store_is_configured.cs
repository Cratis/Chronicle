// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_using_explicit_artifacts;

public class and_a_source_event_store_is_configured : Specification
{
    ReactorScenario<PlainObserver> _scenario;
    DeliverySink _sink;

    void Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<Requested>()
            .RegisterReactor<PlainObserver>(ReactorRegistrationOptions.FromEventStore("host-product"));
        var defaults = new Defaults(new InMemoryClientArtifactsProvider(), null, options);
        _sink = new();
        _scenario = new(defaults);
        _scenario.Services.AddSingleton(_sink);
    }

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new Requested());

    void Destroy() => _scenario.Dispose();

    [Fact] void should_deliver_using_the_configured_inbox() => _sink.Plain.EventSequence.Value.ShouldEqual("inbox-host-product");

    public record Requested;
    public class DeliverySink
    {
        public ReactorDelivery Plain { get; set; }
    }

    public class PlainObserver(DeliverySink sink)
    {
        public void Handle(Requested @event, ReactorDelivery delivery) => sink.Plain = delivery;
    }
}
