// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// The exception that is thrown when a reducer declares more than one method taking an <see cref="Observation.EventBatch"/>.
/// </summary>
/// <param name="reducerType">The reducer <see cref="Type"/>.</param>
public class ReducerHasMoreThanOneBatchMethod(Type reducerType)
    : Exception($"Reducer '{reducerType.FullName}' declares more than one method taking an EventBatch. A reducer is a single fold, so it has one batch method at most.");
