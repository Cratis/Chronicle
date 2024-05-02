// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

#pragma warning disable RCS1047

namespace Cratis.Reflection;

/// <summary>
/// Provides a set of methods for working with methods, such as <see cref="MethodInfo"/>.
/// </summary>
public static class MethodExtensions
{
    /// <summary>
    /// Check whether or not a <see cref="MethodInfo"/> is async or not.
    /// </summary>
    /// <param name="methodInfo"><see cref="MethodInfo"/> to check.</param>
    /// <returns>True if is async, false if not.</returns>
    public static bool IsAsync(this MethodInfo methodInfo)
    {
        return methodInfo.ReturnType.IsAssignableTo(typeof(Task));
    }
}
#pragma warning restore
