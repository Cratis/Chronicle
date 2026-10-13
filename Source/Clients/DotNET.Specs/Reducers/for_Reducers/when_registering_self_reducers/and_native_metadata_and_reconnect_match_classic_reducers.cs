// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reflection;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_self_reducers;

public class and_native_metadata_and_reconnect_match_classic_reducers : an_explicit_store
{
    readonly Dictionary<string, ReducerDefinition> _definitions = [];
    IReducerHandler _before;
    IReducerHandler _after;
    IReducerHandler _classic;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("event-alias"))
            .RegisterReducer<SelfState>("self-alias").RegisterReducer<Classic>("classic-alias")
            .RegisterReducer<PassiveState>().RegisterReducer<InactiveState>();
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message =>
            {
                var definition = message.Content.Value0!.Reducer;
                _definitions[definition.ReducerId] = definition;
            });
            return Observable.Never<ReduceOperationMessage>();
        });
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        await _store.Reducers.Register();
        _before = _store.Reducers.GetHandlerFor(typeof(SelfState));
        _classic = _store.Reducers.GetHandlerFor<Classic>();
        _options.ExplicitArtifacts.RegisterEventType<AnotherEvent>(_ => _.WithId("late-event"));
        typeof(Reducers).GetMethod("RecreateHandlersForReconnect", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_store.Reducers, []);
        _after = _store.Reducers.GetHandlerFor(typeof(SelfState));
    }

    void Destroy()
    {
        foreach (var handler in _store?.Reducers.GetAllHandlers() ?? [])
        {
            handler.Disconnect();
            (handler as IDisposable)?.Dispose();
        }
    }

    [Fact] void should_register_the_self_alias() => _definitions.ContainsKey("self-alias").ShouldBeTrue();
    [Fact] void should_register_the_classic_alias() => _definitions.ContainsKey("classic-alias").ShouldBeTrue();
    [Fact] void should_share_event_identity_and_generation() => _after.EventTypes.ShouldContainOnly(_classic.EventTypes);
    [Fact] void should_use_the_registry_identity() => _after.EventTypes.Single().Id.Value.ShouldEqual("event-alias");
    [Fact] void should_keep_explicit_sequence_priority() => _after.EventSequenceId.ShouldEqual((EventSequenceId)"metadata-sequence");
    [Fact] void should_preserve_descriptive_tags() => _definitions["self-alias"].Tags.ShouldContainOnly("ops", "audit");
    [Fact] void should_preserve_filter_tag_parity() => _definitions["self-alias"].Filters.FilterTags.ShouldContainOnly(_definitions["classic-alias"].Filters.FilterTags);
    [Fact] void should_keep_the_source_filter() => _definitions["self-alias"].Filters.EventSourceType.ShouldEqual("orders");
    [Fact] void should_keep_the_stream_filter() => _definitions["self-alias"].Filters.EventStreamType.ShouldEqual("shipping");
    [Fact] void should_use_the_same_model_and_container_after_reconnect() => _after.ContainerName.ShouldEqual(_before.ContainerName);
    [Fact] void should_keep_the_captured_identity_after_options_change() => _after.Id.Value.ShouldEqual("self-alias");
    [Fact] void should_recreate_the_native_handler() => ReferenceEquals(_before, _after).ShouldBeFalse();
    [Fact] void should_keep_static_invocation_after_reconnect() => _after.Invoker.ShouldBeOfExactType<SelfReducerInvoker>();
    [Fact] void should_keep_classic_invocation_unchanged() => _classic.Invoker.ShouldBeOfExactType<ReducerInvoker>();
    [Fact] void should_not_activate_self_models_during_registration() => _activator.DidNotReceive().Activate(Arg.Any<IServiceProvider>(), typeof(SelfState));
    [Fact] void should_not_register_a_passive_model() => _store.Reducers.GetHandlerFor(typeof(PassiveState)).IsActive.ShouldBeFalse();
    [Fact] void should_not_treat_inactive_as_passive() => typeof(InactiveState).IsPassive().ShouldBeFalse();
    [Fact] void should_not_subscribe_inactive_models() => _store.Reducers.GetHandlerFor(typeof(InactiveState)).IsActive.ShouldBeFalse();
    [Fact] void should_register_only_the_two_active_models() => _definitions.Count.ShouldEqual(2);

    public record ClassicState(string Name);
    [EventSequence("metadata-sequence"), Tags("ops", "audit"), FilterEventsByTag("important"), EventSourceType("orders"), EventStreamType("shipping")]
    public class Classic : IReducerFor<ClassicState>
    {
        public ClassicState Apply(PlainEvent @event, ClassicState? current) => new(@event.Name);
    }
    [EventSequence("metadata-sequence"), Tags("ops", "audit"), FilterEventsByTag("important"), EventSourceType("orders"), EventStreamType("shipping")]
    public record SelfState(string Name)
    {
        public static SelfState Reduce(PlainEvent @event, SelfState? current) => new(@event.Name);
    }
    [Passive]
    public record PassiveState(string Name)
    {
        public static PassiveState Reduce(PlainEvent @event, PassiveState? current) => new(@event.Name);
    }
    [Reducer(isActive: false)]
    public record InactiveState(string Name)
    {
        public static InactiveState Reduce(PlainEvent @event, InactiveState? current) => new(@event.Name);
    }
}
