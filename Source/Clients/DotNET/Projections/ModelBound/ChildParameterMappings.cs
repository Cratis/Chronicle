// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Maps the attributes on the primary constructor parameters of a child type onto the definition of its children collection.
/// </summary>
static class ChildParameterMappings
{
    static readonly Type[] _explicitMappingAttributes =
    [
        typeof(SetFromContextAttribute<>),
        typeof(SetFromAttribute<>),
        typeof(AddFromAttribute<>),
        typeof(SubtractFromAttribute<>),
        typeof(SetValueAttribute<>)
    ];

    /// <summary>
    /// Maps every attribute on a constructor parameter of the child type.
    /// </summary>
    /// <param name="scope">The scope describing the children collection being populated.</param>
    /// <param name="parameter">The constructor parameter to map.</param>
    internal static void Apply(ChildMappingScope scope, ParameterInfo parameter)
    {
        var propertyName = (string)ProjectionPropertyPaths.Resolve(parameter.Name!, scope.Context.NamingPolicy, ProjectionPathRole.Target);

        ApplyFromEvery(scope, parameter, propertyName);
        ApplySetFromContext(scope, parameter, propertyName);

        // [SetValue] is applied twice on purpose: first for every event, then again for the propagated events, so
        // that a propagated [SetValue] keeps taking precedence over other mappings of the same event and property.
        ApplySetValue(scope, parameter, propertyName, parameter.GetAttributesOfGenericType<SetValueAttribute<object>>());
        ApplySetFrom(scope, parameter, propertyName);
        ApplyAddFrom(scope, parameter, propertyName);
        ApplySubtractFrom(scope, parameter, propertyName);
        ApplyCounters(scope, parameter, propertyName);
        ApplySetValue(scope, parameter, propertyName, Propagated<SetValueAttribute<object>>(scope, parameter));
        ApplyClearWith(scope, parameter, propertyName);
        ApplyIdentifierMapping(scope, parameter, propertyName);
        ApplySelfReferencingFromEvents(scope, parameter, propertyName);
        ApplyJoins(scope, parameter, propertyName);
        ApplyRemovals(scope, parameter);
    }

    static IEnumerable<(Attribute Attribute, Type EventType)> Propagated<TAttribute>(ChildMappingScope scope, ParameterInfo parameter) =>
        parameter.GetAttributesOfGenericType<TAttribute>().Where(pair => scope.ShouldPropagate(pair.EventType));

    static string EventPropertyName(Attribute attribute, ParameterInfo parameter) =>
        attribute.ReadString(nameof(SetFromAttribute<object>.EventPropertyName)) ?? parameter.Name!;

    static void ApplyFromEvery(ChildMappingScope scope, ParameterInfo parameter, string propertyName)
    {
        var fromEvery = parameter.GetCustomAttribute<FromEveryAttribute>();
        if (fromEvery is not null)
        {
            ModelBoundProjectionBuilder.AddFromEveryMapping(scope.ChildrenDefinition.All, scope.Context.NamingPolicy, propertyName, fromEvery);
        }
    }

    /// <summary>
    /// The generic event type is honored, so [SetFromContext&lt;OtherEvent&gt;] binds the context mapping to OtherEvent rather
    /// than the child's creating event - matching the property-level path in ModelBoundProjectionBuilder.ProcessMember
    /// and the sibling [SetValue&lt;OtherEvent&gt;] handling.
    /// </summary>
    /// <param name="scope">The scope describing the children collection being populated.</param>
    /// <param name="parameter">The constructor parameter.</param>
    /// <param name="propertyName">The projection property name the parameter maps to.</param>
    static void ApplySetFromContext(ChildMappingScope scope, ParameterInfo parameter, string propertyName)
    {
        foreach (var (attribute, eventType) in parameter.GetAttributesOfGenericType<SetFromContextAttribute<object>>())
        {
            var contextPropertyName = attribute.ReadString(nameof(SetFromContextAttribute<object>.ContextPropertyName)) ?? parameter.Name!;
            scope.ChildrenDefinition.From.AddContextPropertyMapping(
                scope.Context.GetOrCreateEventType,
                scope.Context.NamingPolicy,
                eventType,
                propertyName,
                contextPropertyName,
                attribute is ISetFromContextAttribute { SetOnce: true });
        }
    }

    static void ApplySetValue(ChildMappingScope scope, ParameterInfo parameter, string propertyName, IEnumerable<(Attribute Attribute, Type EventType)> attributes)
    {
        foreach (var (attribute, eventType) in attributes)
        {
            var value = attribute.GetType().GetProperty(nameof(SetValueAttribute<object>.Value))?.GetValue(attribute);
            if (value is not null)
            {
                scope.ChildrenDefinition.From.AddSetValueMapping(scope.Context.GetOrCreateEventType, eventType, propertyName, FromDefinitionExtensions.ConvertValueToInvariantString(value));
            }
            else
            {
                ScalarClear.ThrowIfCannotHoldNull(scope.ChildType, parameter);
                scope.ChildrenDefinition.From.AddClearMapping(scope.Context.GetOrCreateEventType, eventType, propertyName);
            }
        }
    }

    static void ApplySetFrom(ChildMappingScope scope, ParameterInfo parameter, string propertyName)
    {
        foreach (var (attribute, eventType) in Propagated<SetFromAttribute<object>>(scope, parameter))
        {
            scope.ChildrenDefinition.From.AddSetMapping(
                scope.Context.GetOrCreateEventType,
                scope.Context.NamingPolicy,
                eventType,
                propertyName,
                EventPropertyName(attribute, parameter),
                attribute is ISetFromAttribute { SetOnce: true });
        }
    }

    static void ApplyAddFrom(ChildMappingScope scope, ParameterInfo parameter, string propertyName)
    {
        foreach (var (attribute, eventType) in Propagated<AddFromAttribute<object>>(scope, parameter))
        {
            scope.ChildrenDefinition.From.AddAddMapping(scope.Context.GetOrCreateEventType, scope.Context.NamingPolicy, eventType, propertyName, EventPropertyName(attribute, parameter));
        }
    }

    static void ApplySubtractFrom(ChildMappingScope scope, ParameterInfo parameter, string propertyName)
    {
        foreach (var (attribute, eventType) in Propagated<SubtractFromAttribute<object>>(scope, parameter))
        {
            scope.ChildrenDefinition.From.AddSubtractMapping(scope.Context.GetOrCreateEventType, scope.Context.NamingPolicy, eventType, propertyName, EventPropertyName(attribute, parameter));
        }
    }

    static void ApplyCounters(ChildMappingScope scope, ParameterInfo parameter, string propertyName)
    {
        var fromMappings = scope.ChildrenDefinition.From;
        var getOrCreateEventType = scope.Context.GetOrCreateEventType;

        foreach (var (_, eventType) in Propagated<IncrementAttribute<object>>(scope, parameter))
        {
            fromMappings.AddIncrementMapping(getOrCreateEventType, eventType, propertyName);
        }

        foreach (var (_, eventType) in Propagated<DecrementAttribute<object>>(scope, parameter))
        {
            fromMappings.AddDecrementMapping(getOrCreateEventType, eventType, propertyName);
        }

        foreach (var (_, eventType) in Propagated<CountAttribute<object>>(scope, parameter))
        {
            fromMappings.AddCountMapping(getOrCreateEventType, eventType, propertyName);
        }
    }

    /// <summary>
    /// A [Nested] member is handled by ProcessNestedAttributeForChildren, which clears the whole nested object; on any
    /// other member [ClearWith] is a scalar clear.
    /// </summary>
    /// <param name="scope">The scope describing the children collection being populated.</param>
    /// <param name="parameter">The constructor parameter.</param>
    /// <param name="propertyName">The projection property name the parameter maps to.</param>
    static void ApplyClearWith(ChildMappingScope scope, ParameterInfo parameter, string propertyName)
    {
        if (parameter.IsDefined(typeof(NestedAttribute), inherit: false))
        {
            return;
        }

        foreach (var (_, eventType) in Propagated<ClearWithAttribute<object>>(scope, parameter))
        {
            ScalarClear.ThrowIfCannotHoldNull(scope.ChildType, parameter);
            scope.ChildrenDefinition.From.AddClearMapping(scope.Context.GetOrCreateEventType, eventType, propertyName);
        }
    }

    /// <summary>
    /// [ClearWith] is deliberately not an explicit mapping: a clear says what removes a value, never where the value
    /// comes from, so it must not stand in for a mapping source or suppress the diagnostic that reports a missing one.
    /// </summary>
    /// <param name="parameter">The constructor parameter.</param>
    /// <returns><see langword="true"/> if the parameter carries an explicit mapping attribute; otherwise <see langword="false"/>.</returns>
    static bool HasExplicitMapping(ParameterInfo parameter) =>
        parameter.GetCustomAttributes().Any(attribute => _explicitMappingAttributes.Any(attribute.IsClosedFormOf));

    /// <summary>
    /// Maps the identified property, and an independent [Key] parameter, to the key of the creating event.
    /// </summary>
    /// <param name="scope">The scope describing the children collection being populated.</param>
    /// <param name="parameter">The constructor parameter.</param>
    /// <param name="propertyName">The projection property name the parameter maps to.</param>
    static void ApplyIdentifierMapping(ChildMappingScope scope, ParameterInfo parameter, string propertyName)
    {
        if (!scope.ShouldAutoMap || HasExplicitMapping(parameter))
        {
            return;
        }

        if (parameter.Name!.Equals(scope.Identity.IdentifiedBy, StringComparison.OrdinalIgnoreCase))
        {
            MapIdentifiedProperty(scope, parameter, propertyName);
        }
        else if (scope.Identity.Key == WellKnownExpressions.EventSourceId && parameter.GetCustomAttribute<KeyAttribute>() is not null)
        {
            scope.FromDefinition.Properties[propertyName] = $"{WellKnownExpressions.EventContext}(EventSourceId)";
        }
    }

    /// <summary>
    /// Skips the mapping when the key expression matches the parameter name and it has no [Key] attribute. That prevents
    /// auto-mapping properties that are only identifiers, not actual fields needing values.
    /// </summary>
    /// <param name="scope">The scope describing the children collection being populated.</param>
    /// <param name="parameter">The constructor parameter.</param>
    /// <param name="propertyName">The projection property name the parameter maps to.</param>
    static void MapIdentifiedProperty(ChildMappingScope scope, ParameterInfo parameter, string propertyName)
    {
        var keyExpressionMatchesParameter = propertyName.Equals(scope.Identity.KeyExpression, StringComparison.OrdinalIgnoreCase);
        if (keyExpressionMatchesParameter && parameter.GetCustomAttribute<KeyAttribute>() is null)
        {
            return;
        }

        scope.FromDefinition.Properties[propertyName] = scope.Identity.Key == WellKnownExpressions.EventSourceId
            ? $"{WellKnownExpressions.EventContext}(EventSourceId)"
            : scope.Identity.KeyExpression;
    }

    static void ApplySelfReferencingFromEvents(ChildMappingScope scope, ParameterInfo parameter, string propertyName)
    {
        if (!scope.Propagation.IncludeSelfReferencingEvents || !scope.ShouldAutoMap || HasExplicitMapping(parameter))
        {
            return;
        }

        var fromEvents = scope.ChildType
            .GetAttributesOfGenericType<FromEventAttribute<object>>()
            .Where(pair =>
                ChildrenPropagationRules.ShouldPropagateForSelfReference(pair.Attribute, scope.CreatingEventType, scope.Propagation.AncestorCreatorEventTypes) &&
                pair.EventType.GetProperty(parameter.Name!, BindingFlags.Public | BindingFlags.Instance) is not null);

        foreach (var (_, eventType) in fromEvents)
        {
            scope.ChildrenDefinition.From.AddSetMapping(scope.Context.GetOrCreateEventType, scope.Context.NamingPolicy, eventType, propertyName, parameter.Name!);
        }
    }

    static void ApplyJoins(ChildMappingScope scope, ParameterInfo parameter, string propertyName)
    {
        var childrenDef = scope.ChildrenDefinition;
        foreach (var (attribute, eventType) in parameter.GetAttributesOfGenericType<JoinAttribute<object>>())
        {
            childrenDef.Join.ProcessJoinAttribute(scope.Context.GetOrCreateEventType, scope.Context.NamingPolicy, attribute, eventType, parameter.Name!, propertyName, childrenDef.IdentifiedBy);
        }
    }

    /// <summary>
    /// A [RemovedWith] alongside a [ChildrenFrom] on the same parameter is the removal trigger for that nested
    /// (grandchild) collection, not for this child — NestedChildrenWalker routes it to the grandchild definition.
    /// Adding it to this child's definition would remove the whole child item instead.
    /// </summary>
    /// <param name="scope">The scope describing the children collection being populated.</param>
    /// <param name="parameter">The constructor parameter.</param>
    static void ApplyRemovals(ChildMappingScope scope, ParameterInfo parameter)
    {
        if (!parameter.GetAttributesOfGenericType<ChildrenFromAttribute<object>>().Any())
        {
            ChildRemovals.Apply(scope.Context, scope.ChildrenDefinition, parameter);
        }
    }
}
