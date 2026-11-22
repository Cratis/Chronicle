// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Setup.Serialization.for_KernelSerializers;

public class when_passing_immutable_types_within_a_silo : given.the_kernel_serializers
{
    static readonly Type[] _mutableMemberTypes = [typeof(JsonNode), typeof(JsonSchema)];

    EventType _eventType;
    ObserverReaderScope _scope;
    ObserverDefinition _definition;
    ObserverSubscription _subscription;
    object? _passedEventType;
    object? _passedScope;
    object? _passedDefinition;
    object? _passedSubscription;
    List<string> _mutableMembers;
    int _immutableTypeCount;

    void Establish()
    {
        _eventType = new("type", 1);
        _scope = ObserverReaderScope.Explicit([_eventType]);
        _definition = new("observer", [_eventType], "event-log", ObserverType.Reactor, ObserverOwner.Client, true);
        _subscription = ObserverSubscription.Unsubscribed;
    }

    void Because()
    {
        _passedEventType = PassWithinSilo(typeof(EventType), _eventType);
        _passedScope = PassWithinSilo(typeof(ObserverReaderScope), _scope);
        _passedDefinition = PassWithinSilo(typeof(ObserverDefinition), _definition);
        _passedSubscription = PassWithinSilo(typeof(ObserverSubscription), _subscription);
        _mutableMembers = [];

        // Orleans hands over the reference of an immutable type, so one that carries something a receiver could change would
        // let one grain change what another holds.
        var kernelAssemblies = new[] { typeof(EventType).Assembly, typeof(ObserverDefinition).Assembly, typeof(SerializationConfigurationExtensions).Assembly };
        var immutableTypes = kernelAssemblies.SelectMany(_ => _.GetTypes()).Where(_ => _.GetCustomAttributes().Any(a => a.GetType().Name == "ImmutableAttribute")).ToArray();
        _immutableTypeCount = immutableTypes.Length;
        foreach (var type in immutableTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsMutable(property.PropertyType) || (property.SetMethod is { IsPublic: true } setter && !setter.ReturnParameter.GetRequiredCustomModifiers().Any(_ => _.Name == "IsExternalInit")))
                {
                    _mutableMembers.Add($"{type.FullName}.{property.Name}");
                }
            }
        }
    }

    [Fact] void should_share_the_event_type() => ReferenceEquals(_passedEventType, _eventType).ShouldBeTrue();
    [Fact] void should_share_the_reader_scope() => ReferenceEquals(_passedScope, _scope).ShouldBeTrue();
    [Fact] void should_share_the_observer_definition() => ReferenceEquals(_passedDefinition, _definition).ShouldBeTrue();
    [Fact] void should_share_the_observer_subscription() => ReferenceEquals(_passedSubscription, _subscription).ShouldBeTrue();
    [Fact] void should_find_the_immutable_types() => _immutableTypeCount.ShouldBeGreaterThan(4);
    [Fact] void should_only_mark_types_without_mutable_members() => _mutableMembers.ShouldBeEmpty();

    static bool IsMutable(Type type)
    {
        if (type.IsArray || _mutableMemberTypes.Any(_ => _.IsAssignableFrom(type)))
        {
            return true;
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(List<>) ||
               definition == typeof(Dictionary<,>) ||
               definition == typeof(HashSet<>) ||
               definition == typeof(IList<>) ||
               definition == typeof(ICollection<>) ||
               definition == typeof(ISet<>) ||
               definition == typeof(IDictionary<,>);
    }
}
