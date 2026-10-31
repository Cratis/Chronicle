// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// The exception that is thrown when an opted-in self reducer declares no reduction methods.
/// </summary>
/// <param name="readModelType">The self-reducing model.</param>
public class SelfReducerHasNoReduceMethods(Type readModelType)
    : Exception($"Self reducer '{readModelType.FullName}' must declare a public static Reduce method.");
