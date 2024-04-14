// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.ProxyGenerator.Templates;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.ProxyGenerator;

/// <summary>
/// Extension methods for <see cref="ParameterInfo"/>.
/// </summary>
public static class ParameterInfoExtensions
{
    /// <summary>
    /// Convert a <see cref="ParameterInfo"/> to a <see cref="RequestArgumentDescriptor"/>.
    /// </summary>
    /// <param name="parameterInfo">Parameter to convert.</param>
    /// <returns>Converted <see cref="RequestArgumentDescriptor"/>.</returns>
    public static RequestArgumentDescriptor ToRequestArgumentDescriptor(this ParameterInfo parameterInfo)
    {
        var type = parameterInfo.ParameterType.GetTargetType();
        return new RequestArgumentDescriptor(parameterInfo.ParameterType, parameterInfo.Name!, type.Type, parameterInfo.HasDefaultValue);
    }

    /// <summary>
    /// Check if a method is a query method.
    /// </summary>
    /// <param name="parameter">Method to check.</param>
    /// <returns>True if it is a query method, false otherwise.</returns>
    public static bool IsRequestArgument(this ParameterInfo parameter) =>
         parameter.GetCustomAttribute<FromRouteAttribute>() != null ||
         parameter.GetCustomAttribute<FromQueryAttribute>() != null;
}
