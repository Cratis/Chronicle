// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// The exception that is thrown when a self reducer declares an invalid replay lifecycle hook.
/// </summary>
/// <param name="readModelType">The self-reducing model.</param>
/// <param name="method">The invalid hook.</param>
/// <param name="reason">The violated contract.</param>
public class InvalidSelfReducerLifecycleHook(Type readModelType, MethodInfo method, string reason)
    : Exception($"Self reducer '{readModelType.FullName}' declares invalid replay hook '{method.DeclaringType?.FullName}.{method.Name}': {reason}");
