// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Events;
using Aksio.Reflection;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Extension methods for identifying a <see cref="MethodInfo"/> as reducer method.
/// </summary>
public static class ReducerMethodInfoExtensions
{
    /// <summary>
    /// Check if a <see cref="MethodInfo"/> is a reducer method.
    /// </summary>
    /// <param name="methodInfo"><see cref="MethodInfo"/> to check.</param>
    /// <param name="readModelType">Type of read model.</param>
    /// <returns>True if it is a reducer method, false if not.</returns>
    /// <remarks>
    /// The following are considered valid reducer method signatures:
    /// <![CDATA[
    /// Task<TReadModel> {MethodName}(TEvent event, TReadModel current, EventContext context)
    /// Task<TReadModel> {MethodName}(TEvent event, TReadModel current)
    /// TReadModel {MethodName}(TEvent event, TReadModel current, EventContext context)
    /// TReadModel {MethodName}(TEvent event, TReadModel current)
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

        if (parameters.Length >= 2 && parameters[0].ParameterType.HasAttribute<EventTypeAttribute>() && parameters[1].ParameterType.Equals(readModelType))
        {
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
}
