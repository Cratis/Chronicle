// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Graphs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Properties;
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
    ProjectionDefinition _projection;
    object? _passedProjection;
    Dictionary<EventType, FromDefinition> _from;
    Dictionary<PropertyPath, string> _properties;
    List<string> _tags;
    JsonObject _initialState;
    List<string> _mutableValues;
    List<string> _mutableMembers;
    int _immutableTypeCount;

    void Establish()
    {
        _eventType = new("type", 1);
        _scope = ObserverReaderScope.Explicit([_eventType]);
        _definition = new("observer", [_eventType], "event-log", ObserverType.Reactor, ObserverOwner.Client, true);
        _subscription = ObserverSubscription.Unsubscribed;

        // The sources are collections somebody else may keep and change, which a definition must not notice.
        _properties = new() { ["name"] = "Name" };
        _from = new() { [_eventType] = new FromDefinition(_properties, "Key", null) };
        _tags = ["one"];
        _initialState = new JsonObject { ["count"] = 0 };
        var every = new FromEveryDefinition(_properties, true);
        var child = new ChildrenDefinition(
            "id",
            _from,
            new Dictionary<EventType, JoinDefinition> { [_eventType] = new("name", _properties, "Key") },
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            every,
            new Dictionary<EventType, RemovedWithDefinition> { [_eventType] = new("Key", null) },
            new Dictionary<EventType, RemovedWithJoinDefinition> { [_eventType] = new("Key") });
        var graph = new GraphProjectionDefinition(
            new Dictionary<string, GraphNodeProjectionDefinition>
            {
                ["node"] = new(
                    new Dictionary<string, GraphNodeFromDefinition> { ["added"] = new("Key", new Dictionary<string, string> { ["name"] = "Name" }) },
                    new Dictionary<string, GraphNodeRemovedWithDefinition>())
            },
            new Dictionary<string, GraphEdgeProjectionDefinition>());
        _projection = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            "projection",
            "read-model",
            true,
            true,
            _initialState,
            _from,
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition> { ["children"] = child },
            [new FromDerivatives([_eventType], _from[_eventType])],
            every,
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            Tags: _tags,
            Nested: new Dictionary<PropertyPath, ChildrenDefinition> { ["nested"] = child }) { Graph = graph };
    }

    void Because()
    {
        _passedEventType = PassWithinSilo(typeof(EventType), _eventType);
        _passedScope = PassWithinSilo(typeof(ObserverReaderScope), _scope);
        _passedDefinition = PassWithinSilo(typeof(ObserverDefinition), _definition);
        _passedSubscription = PassWithinSilo(typeof(ObserverSubscription), _subscription);
        _passedProjection = PassWithinSilo(typeof(ProjectionDefinition), _projection);

        _from.Clear();
        _properties["name"] = "Changed";
        _tags.Add("two");
        _initialState["count"] = 1;
        _mutableValues = [];
        FindMutableValues("projection", _projection, [], _mutableValues);
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
    [Fact] void should_share_the_projection_definition() => ReferenceEquals(_passedProjection, _projection).ShouldBeTrue();
    [Fact] void should_not_notice_the_collections_it_was_built_from_changing() => _projection.From.Count.ShouldEqual(1);
    [Fact] void should_not_notice_a_property_mapping_changing() => _projection.From[_eventType].Properties["name"].ShouldEqual("Name");
    [Fact] void should_not_notice_tags_changing() => _projection.Tags.ShouldContainOnly("one");
    [Fact] void should_not_notice_the_initial_state_changing() => _projection.InitialModelState.ToJsonObject()["count"]!.GetValue<int>().ShouldEqual(0);
    [Fact] void should_hand_out_an_initial_state_of_its_own() => ReferenceEquals(_projection.InitialModelState.ToJsonObject(), _projection.InitialModelState.ToJsonObject()).ShouldBeFalse();
    [Fact] void should_hold_nothing_that_can_change_anywhere_in_the_projection_definition() => _mutableValues.ShouldBeEmpty();
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

    /// <summary>
    /// Finds what is held rather than what is declared: a read-only interface can be backed by a dictionary somebody else holds.
    /// </summary>
    /// <param name="path">The path to the value.</param>
    /// <param name="value">The value.</param>
    /// <param name="visited">What was visited already.</param>
    /// <param name="mutable">Receives the paths of values that can change.</param>
    static void FindMutableValues(string path, object? value, HashSet<object> visited, List<string> mutable)
    {
        if (value is null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum || value.GetType().IsValueType || !visited.Add(value))
        {
            return;
        }

        switch (value)
        {
            case JsonNode:
            case Array:
                mutable.Add(path);
                return;
            case IDictionary { IsReadOnly: false }:
            case IList { IsReadOnly: false }:
                mutable.Add(path);
                return;
            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                {
                    FindMutableValues($"{path}[{entry.Key}]", entry.Value, visited, mutable);
                }

                return;
            case IEnumerable<object> items:
                var index = 0;
                foreach (var item in items)
                {
                    FindMutableValues($"{path}[{index++}]", item, visited, mutable);
                }

                return;
        }

        if (value.GetType().Namespace?.StartsWith("Cratis.Chronicle.Concepts", StringComparison.Ordinal) != true)
        {
            return;
        }

        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(_ => _.GetIndexParameters().Length == 0))
        {
            FindMutableValues($"{path}.{property.Name}", property.GetValue(value), visited, mutable);
        }
    }
}
