// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// The exception that is thrown when an explicit reducer has no unique supported instance-method read model or contradicts its declared model.
/// </summary>
/// <param name="reducerType">The nominated reducer type.</param>
/// <param name="candidates">The supported method read-model candidates.</param>
public class CannotInferReducerReadModel(Type reducerType, IEnumerable<Type> candidates)
    : Exception($"'{reducerType.FullName}' must have exactly one read model inferred from supported instance (event, current[, EventContext]) methods, agreeing with its declared model. Candidates: {string.Join(", ", candidates.Select(_ => _.FullName))}.");
