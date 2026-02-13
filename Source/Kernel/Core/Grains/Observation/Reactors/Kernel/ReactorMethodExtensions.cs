// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Provides extension methods for reactor methods.
/// </summary>
public static class ReactorMethodExtensions
{
    /// <summary>
    /// Gets the event methods from a collection of methods.
    /// </summary>
    /// <param name="methods">The methods to inspect.</param>
    /// <returns>A collection of methods that represent events.</returns>
    public static IEnumerable<MethodInfo> GetEventMethods(this IEnumerable<MethodInfo> methods) => methods
            .Where(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length == 2 &&
                       parameters[0].ParameterType.IsEventType() &&
                       parameters[1].ParameterType == typeof(EventContext) &&
                       method.ReturnType == typeof(Task);
            })
            .ToArray();

    /// <summary>
    /// Gets the event methods from the reactor type.
    /// </summary>
    /// <param name="reactorType">The reactor type to inspect.</param>
    /// <returns>A collection of methods that represent events.</returns>
    public static IEnumerable<MethodInfo> GetEventMethods(this Type reactorType) =>
        reactorType.GetMethods(BindingFlags.Instance | BindingFlags.Public).GetEventMethods();

    /// <summary>
    /// Gets the event type from the reactor method.
    /// </summary>
    /// <param name="method">The method to inspect.</param>
    /// <returns>The event type.</returns>
    public static Type GetEventType(this MethodInfo method) => method.GetParameters()[0].ParameterType;

    /// <summary>
    /// Gets the event types from the reactor method.
    /// </summary>
    /// <param name="methods">The methods to inspect.</param>
    /// <returns>A collection of event types.</returns>
    public static IEnumerable<Type> GetEventTypes(this IEnumerable<MethodInfo> methods) => methods
        .GetEventMethods()
        .Select(method => method.GetEventType())
        .ToArray();

    /// <summary>
    /// Gets the event types from the reactor type.
    /// </summary>
    /// <param name="reactorType">The reactor type to inspect.</param>
    /// <returns>A collection of event types.</returns>
    public static IEnumerable<EventType> GetEventTypes(this Type reactorType) =>
        reactorType.GetEventMethods().Select(method => method.GetParameters()[0].ParameterType.GetEventType()).ToArray();
}
