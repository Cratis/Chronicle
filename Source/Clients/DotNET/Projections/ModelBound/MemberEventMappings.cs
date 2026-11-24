// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Writes the event mappings declared on a read model member - set, add, subtract, increment, decrement, count,
/// context, constant value, clear and class-level from-event configuration - into its from definitions.
/// </summary>
/// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
/// <param name="namingPolicy">The naming policy for converting property names.</param>
internal sealed class MemberEventMappings(Func<Type, EventType> getOrCreateEventType, INamingPolicy namingPolicy)
{
    /// <summary>
    /// Applies every member-level event mapping of the member.
    /// </summary>
    /// <param name="scope">The <see cref="MemberProcessingScope"/> of the member.</param>
    internal void Apply(MemberProcessingScope scope)
    {
        ApplyPropertyMappings(scope);
        ApplyConstantKeys(scope);
        ApplySetFromContext(scope);
        ApplySetFromEventSourceId(scope);
        ApplySetValue(scope);
        ApplyClearWith(scope);
    }

    /// <summary>
    /// Applies the class-level from-event attributes to the member's from definitions.
    /// </summary>
    /// <param name="scope">The <see cref="MemberProcessingScope"/> of the member.</param>
    internal void ApplyClassLevelFromEvents(MemberProcessingScope scope)
    {
        foreach (var attr in scope.ClassLevelFromEvents)
        {
            ApplyClassLevelFromEventConfiguration(attr, attr.GetType().GetGenericArguments()[0], scope.From);
        }
    }

    void ApplyPropertyMappings(MemberProcessingScope scope)
    {
        MapEventProperties<SetFromAttribute<object>>(scope, (from, eventType, prop, eventProp, attr) => from.AddSetMapping(getOrCreateEventType, namingPolicy, eventType, prop, eventProp, attr is ISetFromAttribute { SetOnce: true }));
        MapEventProperties<AddFromAttribute<object>>(scope, (from, eventType, prop, eventProp, _) => from.AddAddMapping(getOrCreateEventType, namingPolicy, eventType, prop, eventProp));
        MapEventProperties<SubtractFromAttribute<object>>(scope, (from, eventType, prop, eventProp, _) => from.AddSubtractMapping(getOrCreateEventType, namingPolicy, eventType, prop, eventProp));
        MapEventProperties<IncrementAttribute<object>>(scope, (from, eventType, prop, _, _) => from.AddIncrementMapping(getOrCreateEventType, eventType, prop));
        MapEventProperties<DecrementAttribute<object>>(scope, (from, eventType, prop, _, _) => from.AddDecrementMapping(getOrCreateEventType, eventType, prop));
        MapEventProperties<CountAttribute<object>>(scope, (from, eventType, prop, _, _) => from.AddCountMapping(getOrCreateEventType, eventType, prop));
    }

    void MapEventProperties<TAttribute>(MemberProcessingScope scope, Action<IDictionary<EventType, FromDefinition>, Type, string, string, Attribute> mappingAction)
        where TAttribute : Attribute
    {
        var attributes = scope.Member.GetAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(TAttribute).GetGenericTypeDefinition())
            .ToList();

        foreach (var attr in attributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            var eventPropertyNameProperty = attr.GetType().GetProperty(nameof(ICanMapToEventProperty.EventPropertyName));
            var propertyToUse = eventPropertyNameProperty?.GetValue(attr) as string ?? scope.Member.Name;
            if (attr is ICanMapToEventProperty)
            {
                PropertyValidator.ValidatePropertyExists(eventType, propertyToUse);
            }

            mappingAction(scope.From, eventType, scope.PropertyName, propertyToUse, attr);
        }
    }

    void ApplyConstantKeys(MemberProcessingScope scope)
    {
        var provider = scope.Member.Provider;
        ApplyConstantKeyForEventAttributes(provider.GetAttributesOfGenericType<IncrementAttribute<object>>(), scope.From);
        ApplyConstantKeyForEventAttributes(provider.GetAttributesOfGenericType<DecrementAttribute<object>>(), scope.From);
        ApplyConstantKeyForEventAttributes(provider.GetAttributesOfGenericType<CountAttribute<object>>(), scope.From);
    }

    void ApplyConstantKeyForEventAttributes(IEnumerable<(Attribute Attribute, Type EventType)> attributesWithEventType, IDictionary<EventType, FromDefinition> targetFrom)
    {
        foreach (var (attr, eventType) in attributesWithEventType)
        {
            var constantKey = attr switch
            {
                ICountAttribute countAttr => countAttr.ConstantKey,
                IIncrementAttribute incrementAttr => incrementAttr.ConstantKey,
                IDecrementAttribute decrementAttr => decrementAttr.ConstantKey,
                _ => null
            };

            if (!string.IsNullOrEmpty(constantKey))
            {
                targetFrom.GetOrCreateFromDefinition(getOrCreateEventType(eventType)).Key = $"{WellKnownExpressions.Value}({constantKey})";
            }
        }
    }

    void ApplySetFromContext(MemberProcessingScope scope)
    {
        foreach (var (attr, eventType) in scope.Member.Provider.GetAttributesOfGenericType<SetFromContextAttribute<object>>())
        {
            var contextPropertyNameProperty = attr.GetType().GetProperty(nameof(SetFromContextAttribute<object>.ContextPropertyName));
            var propertyToValidate = contextPropertyNameProperty?.GetValue(attr) as string ?? scope.Member.Name;
            var contextPath = PropertyValidator.ValidateContextPropertyPath<EventContext>(propertyToValidate);
            scope.From.AddContextPropertyMapping(getOrCreateEventType, namingPolicy, eventType, scope.PropertyName, contextPath, attr is ISetFromContextAttribute { SetOnce: true });
        }
    }

    void ApplySetFromEventSourceId(MemberProcessingScope scope)
    {
        foreach (var (attr, eventType) in scope.Member.Provider.GetAttributesOfGenericType<SetFromEventSourceIdAttribute<object>>())
        {
            scope.From.AddEventSourceIdMapping(getOrCreateEventType(eventType), scope.PropertyName, attr is ISetFromEventSourceIdAttribute { SetOnce: true });
        }
    }

    void ApplySetValue(MemberProcessingScope scope)
    {
        foreach (var (attr, eventType) in scope.Member.Provider.GetAttributesOfGenericType<SetValueAttribute<object>>())
        {
            var valueProperty = attr.GetType().GetProperty(nameof(SetValueAttribute<object>.Value));
            var value = valueProperty?.GetValue(attr);
            if (value is not null)
            {
                scope.From.AddSetValueMapping(getOrCreateEventType, eventType, scope.PropertyName, FromDefinitionExtensions.ConvertValueToInvariantString(value));
                continue;
            }

            scope.Member.ThrowIfCannotHoldNull();
            scope.From.AddClearMapping(getOrCreateEventType, eventType, scope.PropertyName);
        }
    }

    void ApplyClearWith(MemberProcessingScope scope)
    {
        // A [ClearWith] on a [Nested] single-object member clears the whole nested object and is routed to that
        // nested definition by the nested processing. On any other member it is a scalar clear.
        if (scope.Member.IsNested)
        {
            return;
        }

        foreach (var (_, eventType) in scope.Member.Provider.GetAttributesOfGenericType<ClearWithAttribute<object>>())
        {
            scope.Member.ThrowIfCannotHoldNull();
            scope.From.AddClearMapping(getOrCreateEventType, eventType, scope.PropertyName);
        }
    }

    void ApplyClassLevelFromEventConfiguration(Attribute attr, Type eventType, IDictionary<EventType, FromDefinition> targetFrom)
    {
        var fromDefinition = targetFrom.GetOrCreateFromDefinition(getOrCreateEventType(eventType));

        var key = (attr as IKeyedAttribute)?.Key;
        if (!string.IsNullOrEmpty(key))
        {
            PropertyValidator.ValidatePropertyExists(eventType, key);
            fromDefinition.Key = ProjectionPropertyPaths.Resolve(key, namingPolicy, ProjectionPathRole.Source);
        }

        var parentKey = (attr as IFromEventAttribute)?.ParentKey;
        if (!string.IsNullOrEmpty(parentKey))
        {
            PropertyValidator.ValidatePropertyExists(eventType, parentKey);
            fromDefinition.ParentKey = ProjectionPropertyPaths.Resolve(parentKey, namingPolicy, ProjectionPathRole.Source);
        }

        var constantKey = (attr as IFromEventAttribute)?.ConstantKey;
        if (!string.IsNullOrEmpty(constantKey))
        {
            fromDefinition.Key = $"{WellKnownExpressions.Value}({constantKey})";
        }
    }
}
