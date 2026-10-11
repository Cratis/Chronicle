// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Describes reducer-like methods that were excluded from dispatch.
/// </summary>
internal static class SkippedReducerMethods
{
    /// <summary>
    /// Describe why the public reducer-like methods of a reducer were skipped.
    /// </summary>
    /// <param name="reducerType">The reducer type to inspect.</param>
    /// <param name="readModelType">The type of the current read model.</param>
    /// <param name="eventTypes">Known event types in the process.</param>
    /// <returns>The method signatures and the reasons they were skipped.</returns>
    internal static string Describe(Type reducerType, Type readModelType, IEnumerable<Type> eventTypes) =>
        string.Join("; ", reducerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(method => !method.IsSpecialName && LooksLikeReducerMethod(method, readModelType, eventTypes))
            .Select(method => $"{method}: {GetReason(method, readModelType, eventTypes)}"));

    static bool LooksLikeReducerMethod(MethodInfo method, Type readModelType, IEnumerable<Type> eventTypes) =>
        ReturnsReadModel(method.ReturnType, readModelType) ||
        method.GetParameters().Any(parameter => IsReducerParameter(parameter, readModelType, eventTypes));

    static bool ReturnsReadModel(Type returnType, Type readModelType) =>
        returnType == readModelType || returnType == typeof(Task<>).MakeGenericType(readModelType);

    static bool IsReducerParameter(ParameterInfo parameter, Type readModelType, IEnumerable<Type> eventTypes)
    {
        var type = parameter.ParameterType;
        var valueType = type.IsByRef ? type.GetElementType()! : type;
        return valueType == readModelType || valueType.IsEventType(eventTypes);
    }

    static string GetReason(MethodInfo method, Type readModelType, IEnumerable<Type> eventTypes)
    {
        var parameters = method.GetParameters();
        if (method.IsStatic) return "method must be an instance method";
        if (parameters.Any(parameter => parameter.ParameterType.IsByRef)) return "parameters must not be passed by reference";
        if (parameters.Length is < 2 or > 3) return "method must take two or three parameters";
        if (!parameters[0].ParameterType.IsEventType(eventTypes)) return "first parameter must be a known event type, not the read model or object";
        if (parameters[1].ParameterType != readModelType) return "second parameter must be the current read model";
        if (parameters.Length == 3 && parameters[2].ParameterType != typeof(EventContext)) return "third parameter must be EventContext";
        if (!method.HasReducerMethodShape(readModelType, eventTypes)) return "return type must be the read model, Task of the read model or Task";
        return "event parameter has no registered concrete event types";
    }
}
