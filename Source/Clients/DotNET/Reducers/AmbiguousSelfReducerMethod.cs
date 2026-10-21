// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// The exception that is thrown when an event has multiple equally specific self-reduction methods.
/// </summary>
/// <param name="readModelType">The self-reducing model.</param>
/// <param name="eventType">The event with ambiguous dispatch.</param>
/// <param name="methods">The equally specific methods.</param>
public class AmbiguousSelfReducerMethod(Type readModelType, Type eventType, IEnumerable<MethodInfo> methods)
    : Exception($"Self reducer '{readModelType.FullName}' has ambiguous handlers for '{eventType.FullName}': {string.Join(", ", methods.Select(method => $"{method.DeclaringType?.FullName}.{method}").Order(StringComparer.Ordinal))}.");
