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
    static readonly (Func<Candidate, bool> IsViolated, string Reason)[] _rules =
    [
        (_ => _.Method.IsStatic, "method must be an instance method"),
        (_ => _.Parameters.Any(parameter => parameter.ParameterType.IsByRef), "parameters must not be passed by reference"),
        (_ => _.Parameters.Length is < 2 or > 3, "method must take two or three parameters"),
        (_ => !_.Parameters[0].ParameterType.IsEventType(_.EventTypes), "first parameter must be a known event type, not the read model or object"),
        (_ => _.Parameters[1].ParameterType != _.ReadModelType, "second parameter must be the current read model"),
        (_ => _.Parameters.Length == 3 && _.Parameters[2].ParameterType != typeof(EventContext), "third parameter must be EventContext"),
        (_ => !_.Method.HasReducerMethodShape(_.ReadModelType, _.EventTypes), "return type must be the read model, Task of the read model or Task")
    ];

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
        var candidate = new Candidate(method, method.GetParameters(), readModelType, eventTypes);
        return _rules.FirstOrDefault(rule => rule.IsViolated(candidate)).Reason ?? "event parameter has no registered concrete event types";
    }

    record Candidate(MethodInfo Method, ParameterInfo[] Parameters, Type ReadModelType, IEnumerable<Type> EventTypes);
}
