// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// The exception that is thrown when a self reducer declares an invalid reduction method.
/// </summary>
/// <param name="readModelType">The self-reducing model.</param>
/// <param name="method">The invalid method.</param>
/// <param name="reason">The invalid signature's reason.</param>
public class InvalidSelfReducerSignature(Type readModelType, MethodInfo method, string reason)
    : Exception($"Self reducer '{readModelType.FullName}' has an invalid Reduce method '{method}': {reason}");
