// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// The exception that is thrown when a reducer declares both a method taking an <see cref="Observation.EventBatch"/> and per-event reduce methods.
/// </summary>
/// <param name="reducerType">The reducer <see cref="Type"/>.</param>
public class ReducerMixesBatchAndPerEventMethods(Type reducerType)
    : Exception($"Reducer '{reducerType.FullName}' declares a batch method and per-event reduce methods. A reducer is a single fold over a partition's events in order, so it uses one or the other.");
