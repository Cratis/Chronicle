// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers.Validators;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Extension methods for working with reducer types.
/// </summary>
public static class ReducerTypeExtensions
{
    /// <summary>
    /// Check if a <see cref="MethodInfo"/> is a reducer method.
    /// </summary>
    /// <param name="methodInfo"><see cref="MethodInfo"/> to check.</param>
    /// <param name="readModelType">Type of read model.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <returns>True if it is a reducer method, false if not.</returns>
    /// <remarks>
    /// The following are considered valid reducer method signatures.
    /// <![CDATA[
    /// Task<TReadModel> {MethodName}(TEvent event, TReadModel? initial, EventContext context)
    /// Task<TReadModel> {MethodName}(TEvent event, TReadModel? initial)
    /// TReadModel {MethodName}(TEvent event, TReadModel? current, EventContext context)
    /// TReadModel {MethodName}(TEvent event, TReadModel? current)
    /// ]]>
    /// </remarks>
    public static bool IsReducerMethod(this MethodInfo methodInfo, Type readModelType, IEnumerable<Type> eventTypes)
    {
        if (methodInfo.IsSpecialName)
        {
            return false;
        }

        var returnType = methodInfo.ReturnType;
        var isReducerMethod = returnType == readModelType ||
                              returnType == typeof(Task<>).MakeGenericType(readModelType) ||
                              returnType == typeof(Task) ||
                              IsNullableType(returnType, readModelType) ||
                              IsTaskWithNullableType(returnType, readModelType);

        if (!isReducerMethod) return false;
        var parameters = methodInfo.GetParameters();

        if (parameters.Length > 3) return false;

        if (parameters.Length >= 2 &&
            parameters[0].ParameterType.IsEventType(eventTypes) &&
            parameters[1].ParameterType.Equals(readModelType))
        {
            var nullabilityContext = new NullabilityInfoContext();
            if (nullabilityContext.Create(parameters[1]).ReadState == NullabilityState.NotNull)
            {
                return false;
            }

            if (parameters.Length == 3)
            {
                if (parameters[2].ParameterType == typeof(EventContext)) return true;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get the type of the read model for a reducer.
    /// </summary>
    /// <param name="type">Reducer type to get for.</param>
    /// <returns>Type of read model.</returns>
    public static Type GetReadModelType(this Type type)
    {
        TypeMustImplementReducer.ThrowIfTypeDoesNotImplementReducer(type);
        var interfaces = type.GetInterfaces();
        var reducerInterface = interfaces.Single(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IReducerFor<>));
        return reducerInterface.GetGenericArguments()[0];
    }

    /// <summary>
    /// Get the reducer id for a type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>The <see cref="ReducerId"/> for the type.</returns>
    public static ReducerId GetReducerId(this Type type)
    {
        TypeMustImplementReducer.ThrowIfTypeDoesNotImplementReducer(type);
        var reducerAttribute = type.GetCustomAttribute<ReducerAttribute>();
        var id = reducerAttribute?.Id.Value ?? string.Empty;
        return id switch
        {
            "" => new ReducerId(type.FullName ?? $"{type.Namespace}.{type.Name}"),
            _ => new ReducerId(id)
        };
    }

    /// <summary>
    /// Get the event sequence id for a reducer type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <param name="currentEventStoreName">
    /// The name of the event store the reducer is registered in.
    /// When provided, event types that belong to the same store will resolve to the event log
    /// rather than an inbox sequence.
    /// </param>
    /// <returns>The <see cref="EventSequenceId"/> for the type.</returns>
    /// <exception cref="MultipleEventStoresDefined">Thrown when the reducer handles event types from multiple event stores.</exception>
    /// <exception cref="EventStoreCannotBeCombinedWithExplicitEventSequence">Thrown when <see cref="EventStoreAttribute"/> is combined with explicit event sequence configuration.</exception>
    public static EventSequenceId GetEventSequenceId(this Type type, string? currentEventStoreName = null)
    {
        TypeMustImplementReducer.ThrowIfTypeDoesNotImplementReducer(type);

        var eventStoreAttribute = type.GetCustomAttribute<EventStoreAttribute>();

        var eventSequenceAttr = type.GetCustomAttribute<EventSequenceAttribute>();
        var reducerAttribute = type.GetCustomAttribute<ReducerAttribute>();

        if (eventStoreAttribute is not null)
        {
            if (eventSequenceAttr is not null)
            {
                throw new EventStoreCannotBeCombinedWithExplicitEventSequence(type, eventStoreAttribute.EventStore, eventSequenceAttr.Sequence);
            }

            if (reducerAttribute?.EventSequenceId is not null)
            {
                throw new EventStoreCannotBeCombinedWithExplicitEventSequence(type, eventStoreAttribute.EventStore, reducerAttribute.EventSequenceId);
            }

            return new EventSequenceId($"{EventSequenceId.InboxPrefix}{eventStoreAttribute.EventStore}");
        }

        // [EventSequence] / [EventLog] on the class takes highest priority
        if (eventSequenceAttr is not null)
        {
            return eventSequenceAttr.Sequence;
        }

        if (reducerAttribute?.EventSequenceId is not null)
        {
            return reducerAttribute.EventSequenceId;
        }

        return InferEventSequenceIdFromHandlerMethods(type, currentEventStoreName);
    }

    /// <summary>
    /// Get whether a reducer type has an explicit event sequence set.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns><see langword="true"/> if an explicit event sequence is configured; otherwise <see langword="false"/>.</returns>
    public static bool HasExplicitEventSequence(this Type type)
    {
        TypeMustImplementReducer.ThrowIfTypeDoesNotImplementReducer(type);

        if (Attribute.IsDefined(type, typeof(EventSequenceAttribute)))
        {
            return true;
        }

        var reducerAttribute = type.GetCustomAttribute<ReducerAttribute>();
        return reducerAttribute?.EventSequenceId is not null;
    }

    /// <summary>
    /// Get whether a reducer is active.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>True if it is, false if not.</returns>
    public static bool IsActive(this Type type)
    {
        TypeMustImplementReducer.ThrowIfTypeDoesNotImplementReducer(type);
        var reducerAttribute = type.GetCustomAttribute<ReducerAttribute>();
        return reducerAttribute?.IsActive ?? true;
    }

    /// <summary>
    /// Get all event types used in the handler method signatures of a reducer type.
    /// </summary>
    /// <remarks>
    /// A handler method is any non-special public or non-public instance method whose first parameter type
    /// carries the <see cref="EventTypeAttribute"/>. Duplicates are removed so each event type appears at most once.
    /// </remarks>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>All event <see cref="Type">types</see> found as first parameters in handler methods.</returns>
    public static IEnumerable<Type> GetHandlerEventTypes(this Type type) =>
        type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.GetParameters().FirstOrDefault()?.ParameterType)
            .Where(t => t is not null && Attribute.IsDefined(t, typeof(EventTypeAttribute)))
            .Select(t => t!)
            .Distinct()
            .ToList();

    static EventSequenceId InferEventSequenceIdFromHandlerMethods(Type reducerType, string? currentEventStoreName)
    {
        var eventParameterTypes = reducerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.GetParameters().FirstOrDefault()?.ParameterType)
            .Where(t => t is not null && Attribute.IsDefined(t, typeof(EventTypeAttribute)))
            .Distinct()
            .ToList();

        var eventStores = eventParameterTypes
            .Select(t => t!.GetEventStoreName())
            .Where(name => name is not null)
            .Select(name => name!)
            .Distinct()
            .ToList();

        if (eventStores.Count > 1)
        {
            throw new MultipleEventStoresDefined(reducerType, eventStores);
        }

        if (eventStores.Count == 1)
        {
            // If the event types belong to the same store as the one we're in, use event-log instead of inbox
            if (currentEventStoreName is not null && eventStores[0] == currentEventStoreName)
            {
                return EventSequenceId.Log;
            }

            return new EventSequenceId($"{EventSequenceId.InboxPrefix}{eventStores[0]}");
        }

        return EventSequenceId.Log;
    }

    static bool IsNullableType(Type returnType, Type readModelType)
    {
        // Check if it's a nullable reference type in a nullable context
        if (returnType == readModelType)
        {
            return true; // This covers both nullable and non-nullable reference types
        }

        return false;
    }

    static bool IsTaskWithNullableType(Type returnType, Type readModelType)
    {
        // Check if it's Task<ReadModel?> where ReadModel is a reference type
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = returnType.GetGenericArguments()[0];
            return innerType == readModelType; // This covers both nullable and non-nullable reference types
        }

        return false;
    }
}
