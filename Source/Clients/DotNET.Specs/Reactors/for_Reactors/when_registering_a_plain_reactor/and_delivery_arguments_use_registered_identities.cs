// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_a_plain_reactor;

public class and_delivery_arguments_use_registered_identities : an_explicit_store
{
    readonly Dictionary<string, Subject<EventsToObserve>> _streams = [];
    DeliverySink _sink;
    System.Diagnostics.ActivitySource _trace;
    EventContext _context;
    ReactorDelivery _plain;
    ReactorDelivery _marked;
    ReactorDelivery _publicPlain;
    ReactorDelivery _publicMarked;

    void Establish()
    {
        _provider.Dispose();
        _sink = new();
        _trace = new("registered-delivery");
        var services = new ServiceCollection().AddSingleton(_sink);
        services.AddKeyedSingleton<IActivitySource<EventSequence>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<EventSequence>>());
        services.AddKeyedSingleton<IActivitySource<Reactors>>(ClientActivity.SourceName, (_, _) => new Cratis.Traces.ActivitySource<Reactors>(_trace));
        services.AddKeyedSingleton<IActivitySource<Reducers.Reducers>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reducers.Reducers>>());
        _provider = services.BuildServiceProvider();
        _activator = new ClientArtifactsActivator(_provider, NullLoggerFactory.Instance);
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("delivery-event"))
            .RegisterReactor<PlainObserver>("plain-delivery-alias").RegisterReactor<MarkedObserver>("marked-delivery-alias");
        _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            string id = null!;
            call.Arg<IObservable<ReactorMessage>>().Take(1).Subscribe(message =>
            {
                id = message.Content.Value0!.Reactor.ReactorId;
                _streams[id] = new();
            });
            return _streams[id];
        });
        _store = CreateStore();
        _context = EventContext.From("current", "default", new("delivery-event", 1), EventSourceType.Default, "partition", EventStreamType.All, EventStreamId.Default, 42UL, CorrelationId.NotSet);
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        await _store.Reactors.Register();
        var resolver = new ReactorMethodArgumentsResolver();
        var plainArguments = await resolver.Resolve(typeof(PlainObserver).GetMethod("Handle")!, new PlainObserver(_sink), new PlainEvent("direct"), _context, _store, null);
        var markedArguments = await resolver.Resolve(typeof(MarkedObserver).GetMethod("Handle")!, new MarkedObserver(_sink), new PlainEvent("direct"), _context, _store, null);
        _publicPlain = (ReactorDelivery)plainArguments[1]!;
        _publicMarked = (ReactorDelivery)markedArguments[1]!;
        foreach (var stream in _streams.Values)
        {
            stream.OnNext(new EventsToObserve
            {
                Partition = "partition",
                Events = [new Contracts.Events.AppendedEvent { Context = _context.ToContract(), Content = "{\"Name\":\"delivered\"}" }]
            });
        }
        _plain = await _sink.Plain.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _marked = await _sink.Marked.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy()
    {
        foreach (var type in new[] { typeof(PlainObserver), typeof(MarkedObserver) }) _store.Reactors.GetHandlerFor(type).Disconnect();
        foreach (var stream in _streams.Values) stream.Dispose();
        _trace.Dispose();
    }

    [Fact] void should_deliver_to_the_plain_reactor_using_its_registered_alias() => _plain.ShouldEqual(ReactorDelivery.For("plain-delivery-alias", "delivery-sequence", _context));
    [Fact] void should_deliver_to_the_marker_reactor_using_its_registered_alias() => _marked.ShouldEqual(ReactorDelivery.For("marked-delivery-alias", "delivery-sequence", _context));
    [Fact] void should_resolve_plain_delivery_through_the_public_resolver_using_the_registered_alias() => _publicPlain.ShouldEqual(_plain);
    [Fact] void should_resolve_marker_delivery_through_the_public_resolver_using_the_registered_alias() => _publicMarked.ShouldEqual(_marked);
    [Fact] void should_keep_public_delivery_helpers_declaration_only() => ReactorDelivery.For(new MarkedObserver(_sink), _context).Reactor.Value.ShouldEqual(typeof(MarkedObserver).FullName);

    public class DeliverySink
    {
        public TaskCompletionSource<ReactorDelivery> Plain { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<ReactorDelivery> Marked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [EventSequence("delivery-sequence")]
    public class PlainObserver(DeliverySink sink)
    {
        public void Handle(PlainEvent @event, ReactorDelivery delivery) => sink.Plain.TrySetResult(delivery);
    }

    [EventSequence("delivery-sequence")]
    public class MarkedObserver(DeliverySink sink) : IReactor
    {
        public void Handle(PlainEvent @event, ReactorDelivery delivery) => sink.Marked.TrySetResult(delivery);
    }
}
