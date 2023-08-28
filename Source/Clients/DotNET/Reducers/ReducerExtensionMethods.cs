// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Events;
using Aksio.Cratis.Reducers.Validators;
using Aksio.Reflection;

namespace Aksio.Cratis.Reducers;

#nullable disable

/// <summary>
/// Extension methods for identifying a <see cref="MethodInfo"/> as reducer method.
/// </summary>
public static class ReducerExtensionMethods
{
    /// <summary>
    /// Check if a <see cref="MethodInfo"/> is a reducer method.
    /// </summary>
    /// <param name="methodInfo"><see cref="MethodInfo"/> to check.</param>
    /// <param name="readModelType">Type of read model.</param>
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
    public static bool IsReducerMethod(this MethodInfo methodInfo, Type readModelType)
    {
        var isReducerMethod = methodInfo.ReturnType == readModelType ||
                              methodInfo.ReturnType == typeof(Task<>).MakeGenericType(readModelType) ||
                              methodInfo.ReturnType == typeof(void);

        if (!isReducerMethod) return false;
        var parameters = methodInfo.GetParameters();

        if (parameters.Length > 3) return false;

        if (parameters.Length >= 2 &&
            parameters[0].ParameterType.HasAttribute<EventTypeAttribute>() &&
            parameters[1].ParameterType.Equals(readModelType))
        {
            if (methodInfo.DeclaringType.IsNullableContext() && !parameters[1].IsNullableReferenceType())
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
    /// Check whether or not a type is in a nullable context.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is in a nullable context, false if not.</returns>
    public static bool IsNullableContext(this Type type) =>
        type.GetCustomAttributes().Any(_ => _.GetType().FullName == "System.Runtime.CompilerServices.NullableContextAttribute");

    /// <summary>
    /// Check whether or not a member that implements <see cref="ICustomAttributeProvider"/> is a nullable reference type.
    /// </summary>
    /// <param name="member">Member that implements <see cref="ICustomAttributeProvider"/> to check.</param>
    /// <returns>True if it nullable, false if not.</returns>
    public static bool IsNullableReferenceType(this ICustomAttributeProvider member) =>
        member.GetCustomAttributes(false).Any(_ => _.GetType().FullName == "System.Runtime.CompilerServices.NullableAttribute");
}
