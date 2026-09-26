// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Extension methods for working with event handler methods.
/// </summary>
public static class EventHandlerMethods
{
    /// <summary>
    /// Check if a <see cref="MethodInfo"/> is an event handler method.
    /// </summary>
    /// <param name="methodInfo"><see cref="MethodInfo"/> to check.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <returns>True if it is, false if not.</returns>
    /// <remarks>
    /// A handler method is identified solely by its first parameter being a known event type. Any further
    /// parameters are treated as dependencies (the <see cref="EventContext"/>, read models, or services) and
    /// are resolved when the method is invoked.
    /// </remarks>
    public static bool IsEventHandlerMethod(this MethodInfo methodInfo, IEnumerable<Type> eventTypes)
    {
        if (methodInfo.IsSpecialName)
        {
            return false;
        }

        var eventTypesList = eventTypes as IList<Type> ?? [.. eventTypes];

        var hasValidReturnType = methodInfo.ReturnType.IsAssignableTo(typeof(Task)) ||
                                    methodInfo.ReturnType == typeof(void) ||
                                    IsValidSyncSideEffectReturnType(methodInfo.ReturnType, eventTypesList);

        if (!hasValidReturnType)
        {
            return false;
        }

        var parameters = methodInfo.GetParameters();
        return parameters.Length >= 1 && parameters[0].ParameterType.IsEventType(eventTypesList);
    }

    /// <summary>
    /// Check whether a <see cref="MethodInfo"/> has the shape of an event handler method — a first parameter
    /// that is a known event type, with an optional <see cref="EventContext"/> or <see cref="ReactorDelivery"/>
    /// as the second parameter — regardless of whether its return type is a supported side-effect return type.
    /// </summary>
    /// <param name="methodInfo"><see cref="MethodInfo"/> to check.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <returns>True if the method is shaped like an event handler, false if not.</returns>
    public static bool HasEventHandlerShape(this MethodInfo methodInfo, IEnumerable<Type> eventTypes)
    {
        if (methodInfo.IsSpecialName)
        {
            return false;
        }

        var parameters = methodInfo.GetParameters();
        if (parameters.Length is < 1 or > 2)
        {
            return false;
        }

        var eventTypesList = eventTypes as IList<Type> ?? [.. eventTypes];
        if (!parameters[0].ParameterType.IsEventType(eventTypesList))
        {
            return false;
        }

        return parameters.Length == 1 ||
               parameters[1].ParameterType == typeof(EventContext) ||
               parameters[1].ParameterType == typeof(ReactorDelivery);
    }

    /// <summary>
    /// Check whether a <see cref="Type"/> is a valid synchronous side-effect return type for a reactor handler method.
    /// Valid types are: a registered event type, <see cref="EventForEventSourceId"/>,
    /// <see cref="EventsWithConcurrencyScopes"/>, or any sequence of either a registered event type or
    /// <see cref="EventForEventSourceId"/>.
    /// </summary>
    /// <param name="returnType">The return <see cref="Type"/> to check.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <returns>True if it is a valid sync side-effect return type, false if not.</returns>
    /// <remarks>
    /// Any sequence counts, not only a declared <see cref="IEnumerable{T}"/>. The side-effect handlers test the
    /// returned value with <c language="csharp">value is IEnumerable&lt;object&gt;</c>, so an array or a List is
    /// handled perfectly well once it reaches them - but this check ran first and matched the open generic
    /// exactly, so a handler returning one was never recognized as a handler at all and the reactor simply never
    /// fired for that event, with nothing said (#4121). The asynchronous path accepts any <see cref="Task{T}"/>
    /// without inspecting T, so the two shapes now agree.
    /// </remarks>
    public static bool IsValidSyncSideEffectReturnType(Type returnType, IEnumerable<Type> eventTypes)
    {
        if (returnType == typeof(EventForEventSourceId)) return true;
        if (returnType == typeof(EventsWithConcurrencyScopes)) return true;
        if (eventTypes.Contains(returnType)) return true;

        return GetSequenceElementTypes(returnType).Any(elementType =>
            elementType == typeof(object) ||
            elementType == typeof(EventForEventSourceId) ||
            eventTypes.Contains(elementType));
    }

    /// <summary>
    /// Get the element types of every <see cref="IEnumerable{T}"/> a <see cref="Type"/> represents or implements.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to get for.</param>
    /// <returns>The element types, empty when the type is not a sequence.</returns>
    static IEnumerable<Type> GetSequenceElementTypes(Type type)
    {
        if (type == typeof(string))
        {
            return [];
        }

        if (type.IsArray)
        {
            return type.GetElementType() is { } elementType ? [elementType] : [];
        }

        var interfaces = type.IsInterface ? new[] { type }.Concat(type.GetInterfaces()) : type.GetInterfaces();
        return interfaces
            .Where(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(candidate => candidate.GetGenericArguments()[0]);
    }
}
