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
    public static bool IsEventHandlerMethod(this MethodInfo methodInfo, IEnumerable<Type> eventTypes)
    {
        if (methodInfo.IsSpecialName)
        {
            return false;
        }

        var eventTypesList = eventTypes as IList<Type> ?? [.. eventTypes];

        var isEventHandlerMethod = methodInfo.ReturnType.IsAssignableTo(typeof(Task)) ||
                                    methodInfo.ReturnType == typeof(void) ||
                                    IsValidSyncSideEffectReturnType(methodInfo.ReturnType, eventTypesList);

        if (!isEventHandlerMethod) return false;
        var parameters = methodInfo.GetParameters();
        if (parameters.Length >= 1)
        {
            isEventHandlerMethod = parameters[0].ParameterType.IsEventType(eventTypesList);
            if (parameters.Length == 2)
            {
                isEventHandlerMethod &= parameters[1].ParameterType == typeof(EventContext);
            }
            else if (parameters.Length > 2)
            {
                isEventHandlerMethod = false;
            }
            return isEventHandlerMethod;
        }

        return false;
    }

    /// <summary>
    /// Check whether a <see cref="Type"/> is a valid synchronous side-effect return type for a reactor handler method.
    /// Valid types are: a registered event type, <see cref="EventForEventSourceId"/>, or <see cref="IEnumerable{T}"/> of
    /// either a registered event type or <see cref="EventForEventSourceId"/>.
    /// </summary>
    /// <param name="returnType">The return <see cref="Type"/> to check.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <returns>True if it is a valid sync side-effect return type, false if not.</returns>
    public static bool IsValidSyncSideEffectReturnType(Type returnType, IEnumerable<Type> eventTypes)
    {
        if (returnType == typeof(EventForEventSourceId)) return true;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = returnType.GetGenericArguments()[0];
            if (elementType == typeof(object)) return true;
            if (elementType == typeof(EventForEventSourceId)) return true;
            if (eventTypes.Contains(elementType)) return true;
        }

        return eventTypes.Contains(returnType);
    }
}
