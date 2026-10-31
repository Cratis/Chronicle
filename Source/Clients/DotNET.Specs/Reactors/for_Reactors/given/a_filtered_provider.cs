// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Reactors.for_Reactors.given;

public class a_filtered_provider : all_dependencies
{
    protected readonly List<(string Store, EventTypeId Id)> _subscriptions = [];
    protected readonly EventType _registeredIdentity = new("filtered-handler", 2);
    protected IReactorHandler _handler;
    protected IEventTypes _registry;
    protected virtual Type HandlerEvent => typeof(LocalEvent);
    protected virtual bool IncludeMissingHandler => false;

    void Establish()
    {
        _registry = RegisteredEventTypesForSpecifications.Create((HandlerEvent, _registeredIdentity));
        _eventStore.EventTypes.Returns(_registry);
        _eventStore.Name.Returns((EventStoreName)"current");
        _clientArtifactsProvider.Reactors.Returns([CreateReactor()]);
        _clientArtifactsProvider.Reducers.Returns([]);
    }

    protected async Task DiscoverAndSubscribe()
    {
        await _reactors.Discover();
        _handler = _reactors.Handlers.Single();
        var subscriptions = Substitute.For<IEventStoreSubscriptions>();
        subscriptions.Subscribe(Arg.Any<EventStoreSubscriptionId>(), Arg.Any<string>(), Arg.Any<Action<IEventStoreSubscriptionBuilder>>())
            .Returns(call =>
            {
                var builder = Substitute.For<IEventStoreSubscriptionBuilder>();
                builder.WithEventType(Arg.Any<EventTypeId>()).Returns(builder).AndDoes(configuration =>
                    _subscriptions.Add((call.Arg<string>(), configuration.Arg<EventTypeId>())));
                call.Arg<Action<IEventStoreSubscriptionBuilder>>()(builder);
                return Task.CompletedTask;
            });
        var projections = Substitute.For<IProjections>();
        projections.GetExternalEventStoreSubscriptions().Returns([]);
        var store = (EventStore)RuntimeHelpers.GetUninitializedObject(typeof(EventStore));
        Set(store, "_eventStoreName", new EventStoreName("current"));
        Set(store, "_clientArtifactsProvider", _clientArtifactsProvider);
        Set(store, "<EventTypes>k__BackingField", _registry);
        Set(store, "<Reactors>k__BackingField", _reactors);
        Set(store, "<Reducers>k__BackingField", Substitute.For<IReducers>());
        Set(store, "<Projections>k__BackingField", projections);
        Set(store, "<Subscriptions>k__BackingField", subscriptions);
        await (Task)typeof(EventStore).GetMethod("RegisterExternalEventStoreSubscriptionsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(store, [])!;
    }

    Type CreateReactor()
    {
        // Emit the deliberately invalid variant at runtime so assembly-wide discovery remains valid.
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"FilteredReactor_{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect);
        var type = assembly.DefineDynamicModule("Fixtures").DefineType("FilteredReactor", TypeAttributes.Public);
        type.AddInterfaceImplementation(typeof(IReactor));
        type.DefineDefaultConstructor(MethodAttributes.Public);
        DefineHandler(type, "Handle", HandlerEvent);
        var helper = type.DefineMethod("IsExcluded", MethodAttributes.Private, typeof(bool), [typeof(ExcludedMarkedEvent)]).GetILGenerator();
        helper.Emit(OpCodes.Ldc_I4_0);
        helper.Emit(OpCodes.Ret);
        if (IncludeMissingHandler)
        {
            DefineHandler(type, "HandleMissing", typeof(ExcludedMarkedEvent));
        }

        return type.CreateType();
    }

    static void DefineHandler(TypeBuilder type, string name, Type eventType)
    {
        var method = type.DefineMethod(name, MethodAttributes.Public, typeof(Task), [eventType]).GetILGenerator();
        method.Emit(OpCodes.Call, typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetMethod!);
        method.Emit(OpCodes.Ret);
    }

    static void Set(EventStore store, string field, object value) =>
        typeof(EventStore).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(store, value);

    [EventType("filtered-local-event")]
    public record LocalEvent;
    [EventType("filtered-foreign-event")]
    [EventStore("source")]
    public record ForeignEvent;
    [EventType("filtered-excluded-event")]
    [EventStore("ignored")]
    public record ExcludedMarkedEvent;
}
