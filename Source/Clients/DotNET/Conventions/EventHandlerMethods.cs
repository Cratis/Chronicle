// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Conventions;

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
        var isEventHandlerMethod = (methodInfo.ReturnType.IsAssignableTo(typeof(Task)) && !methodInfo.ReturnType.IsGenericType) ||
                                    methodInfo.ReturnType == typeof(void);

        if (!isEventHandlerMethod) return false;
        var parameters = methodInfo.GetParameters();
        if (parameters.Length >= 1)
        {
            isEventHandlerMethod = parameters[0].ParameterType.IsEventType(eventTypes);
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
}
