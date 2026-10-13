// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Registrations;

namespace Cratis.Chronicle.Specs.for_EventStore.when_registering_external_event_store_subscriptions;

public class and_self_dispatch_matches_classic_source_inference : Specification
{
    readonly List<(string Store, EventTypeId Id)> _subscriptions = [];
    readonly List<EventSequenceId> _sequences = [];
    IEventTypes _registry;
    Exception _multipleStores;

    void Establish() => _registry = RegisteredEventTypesForSpecifications.Create((typeof(ForeignFirst), new("first-alias", 4)), (typeof(ForeignSecond), new("second-alias", 2)));

    async Task Because()
    {
        foreach (var type in new[] { typeof(Classic), typeof(SelfState), typeof(CustomSequence), typeof(StoreOverride) })
        {
            var self = ReducerCatalog.IsSelfReducer(type);
            var model = self ? type : typeof(ClassicState);
            var invoker = ReducerInvokerSelector.Create(new(type, model, self), _registry, Substitute.For<IClientArtifactsActivator>(), "state");
            _sequences.Add(RegisteredObserverMetadata.SequenceFor(type, type.GetCustomAttribute<ReducerAttribute>()?.EventSequenceId, _registry, "current", () => invoker.EventTypes));
            await Register("current", type, invoker);
            await Register("other", type, invoker);
        }
        var mixed = RegisteredEventTypesForSpecifications.Create((typeof(ForeignFirst), new("first", 1)), (typeof(ThirdStore), new("third", 1)));
        var mixedInvoker = new SelfReducerInvoker(mixed, typeof(SelfState), "state");
        _multipleStores = Catch.Exception(() => RegisteredObserverMetadata.SequenceFor(typeof(SelfState), null, mixed, "current", () => mixedInvoker.EventTypes));
    }

    async Task Register(string currentStore, Type type, IReducerInvoker invoker)
    {
        var subscriptions = Substitute.For<IEventStoreSubscriptions>();
        subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>()).Returns(call =>
        {
            var builder = Substitute.For<IEventStoreSubscriptionBuilder>();
            builder.WithEventType(Arg.Any<EventTypeId>()).Returns(builder).AndDoes(configuration => _subscriptions.Add((call.Arg<string>(), configuration.Arg<EventTypeId>())));
            call.Arg<Action<IEventStoreSubscriptionBuilder>>()(builder);
            return Task.CompletedTask;
        });
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.Reducers.Returns([type]);
        var reducers = Substitute.For<IReducers>();
        var handler = Substitute.For<IReducerHandler>();
        handler.ReducerType.Returns(type);
        handler.EventTypes.Returns(invoker.EventTypes);
        reducers.GetAllHandlers().Returns([handler]);
        var projections = Substitute.For<IProjections>();
        projections.GetExternalEventStoreSubscriptions().Returns([]);
        var store = (EventStore)RuntimeHelpers.GetUninitializedObject(typeof(EventStore));
        Set(store, "_eventStoreName", new EventStoreName(currentStore));
        Set(store, "_clientArtifactsProvider", artifacts);
        Set(store, "<EventTypes>k__BackingField", _registry);
        Set(store, "<Reactors>k__BackingField", Substitute.For<IReactors>());
        Set(store, "<Reducers>k__BackingField", reducers);
        Set(store, "<Projections>k__BackingField", projections);
        Set(store, "<Subscriptions>k__BackingField", subscriptions);
        await (Task)typeof(EventStore).GetMethod("RegisterExternalEventStoreSubscriptionsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(store, [])!;
    }

    static void Set(EventStore store, string field, object value) => typeof(EventStore).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(store, value);

    [Fact] void should_infer_foreign_inbox_for_classic_and_self() => _sequences.Take(2).All(sequence => sequence.Value == "inbox-other").ShouldBeTrue();
    [Fact] void should_prioritize_an_explicit_sequence() => _sequences[2].ShouldEqual((EventSequenceId)"custom");
    [Fact] void should_infer_inbox_from_the_observer_store_override() => _sequences[3].Value.ShouldEqual("inbox-override");
    [Fact] void should_subscribe_each_concrete_dispatch_identity_without_helpers() => _subscriptions.Count.ShouldEqual(8);
    [Fact] void should_match_classic_foreign_identity_union() => _subscriptions.Count(subscription => subscription == ("other", new EventTypeId("first-alias"))).ShouldEqual(2);
    [Fact] void should_honor_the_observer_override_in_both_current_stores() => _subscriptions.Count(subscription => subscription.Store == "override").ShouldEqual(4);
    [Fact] void should_reject_multiple_implicit_source_stores() => _multipleStores.ShouldBeOfExactType<MultipleEventStoresDefined>();

    public interface IForeign;
    [EventStore("other")] public record ForeignFirst : IForeign;
    [EventStore("other")] public record ForeignSecond : IForeign;
    [EventStore("third")] public record ThirdStore : IForeign;
    public record ClassicState;
    public class Classic : IReducerFor<ClassicState>
    {
        public ClassicState Apply(IForeign @event, ClassicState? current) => new();
    }
    public record SelfState
    {
        public static SelfState Reduce(IForeign @event, SelfState? current) => new();
        public static SelfState Query(ThirdStore @event) => new();
    }
    [EventSequence("custom")]
    public record CustomSequence
    {
        public static CustomSequence Reduce(IForeign @event, CustomSequence? current) => new();
    }
    [EventStore("override")]
    public record StoreOverride
    {
        public static StoreOverride Reduce(IForeign @event, StoreOverride? current) => new();
    }
}
