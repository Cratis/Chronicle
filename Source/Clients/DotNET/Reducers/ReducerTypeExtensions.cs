// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers.Validators;
using Cratis.Reflection;

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
        var isReducerMethod = methodInfo.ReturnType == readModelType ||
                              methodInfo.ReturnType == typeof(Task<>).MakeGenericType(readModelType);

        if (!isReducerMethod) return false;
        var parameters = methodInfo.GetParameters();

        if (parameters.Length > 3) return false;

        if (parameters.Length >= 2 &&
            parameters[0].ParameterType.IsEventType(eventTypes) &&
            parameters[1].ParameterType.Equals(readModelType))
        {
            if ((methodInfo.DeclaringType?.IsNullableContext() ?? false) && !parameters[1].IsNullableReferenceType())
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
    /// <returns>The <see cref="EventSequenceId"/> for the type.</returns>
    public static EventSequenceId GetEventSequenceId(this Type type)
    {
        TypeMustImplementReducer.ThrowIfTypeDoesNotImplementReducer(type);
        var reducerAttribute = type.GetCustomAttribute<ReducerAttribute>();
        return reducerAttribute?.EventSequenceId.Value ?? EventSequenceId.Log;
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
}
