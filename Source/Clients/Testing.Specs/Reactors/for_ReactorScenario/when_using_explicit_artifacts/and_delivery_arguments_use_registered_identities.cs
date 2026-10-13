// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_using_explicit_artifacts;

public class and_delivery_arguments_use_registered_identities : Specification
{
    ReactorScenario<PlainObserver> _plainScenario;
    ReactorScenario<MarkedObserver> _markedScenario;
    DeliverySink _sink;
    EventSourceId _partition;

    void Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<Requested>()
            .RegisterReactor<PlainObserver>("plain-delivery-alias").RegisterReactor<MarkedObserver>("marked-delivery-alias");
        var defaults = new Defaults(new InMemoryClientArtifactsProvider(), null, options);
        _sink = new();
        _partition = EventSourceId.New();
        _plainScenario = new(defaults);
        _markedScenario = new(defaults);
        _plainScenario.Services.AddSingleton(_sink);
        _markedScenario.Services.AddSingleton(_sink);
    }

    async Task Because()
    {
        await _plainScenario.Given.ForEventSource(_partition).Events(new Requested());
        await _markedScenario.Given.ForEventSource(_partition).Events(new Requested());
    }

    void Destroy()
    {
        _plainScenario.Dispose();
        _markedScenario.Dispose();
    }

    [Fact] void should_deliver_to_the_plain_reactor_using_its_registered_alias() => _sink.Plain.Reactor.Value.ShouldEqual("plain-delivery-alias");
    [Fact] void should_deliver_to_the_marker_reactor_using_its_registered_alias() => _sink.Marked.Reactor.Value.ShouldEqual("marked-delivery-alias");
    [Fact] void should_use_the_registered_sequence_for_the_plain_reactor() => _sink.Plain.EventSequence.Value.ShouldEqual("delivery-sequence");
    [Fact] void should_use_the_registered_sequence_for_the_marker_reactor() => _sink.Marked.EventSequence.Value.ShouldEqual("delivery-sequence");
    [Fact] void should_keep_the_partition() => _sink.Plain.Partition.ShouldEqual(_partition);
    [Fact] void should_keep_the_sequence_number() => _sink.Plain.SequenceNumber.ShouldEqual(EventSequenceNumber.First);

    public record Requested;
    public class DeliverySink
    {
        public ReactorDelivery Plain { get; set; }
        public ReactorDelivery Marked { get; set; }
    }

    [EventSequence("delivery-sequence")]
    public class PlainObserver(DeliverySink sink)
    {
        public void Handle(Requested @event, ReactorDelivery delivery) => sink.Plain = delivery;
    }

    [EventSequence("delivery-sequence")]
    public class MarkedObserver(DeliverySink sink) : IReactor
    {
        public void Handle(Requested @event, ReactorDelivery delivery) => sink.Marked = delivery;
    }
}
