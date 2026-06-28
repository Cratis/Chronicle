// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// The exception that is thrown when a reducer raises an error while processing events in a <see cref="ReadModelScenario{TReadModel}"/>.
/// </summary>
/// <param name="readModelType">The read model type whose reducer failed.</param>
/// <param name="errorMessages">The error messages reported by the reducer.</param>
/// <param name="stackTrace">The stack trace reported by the reducer, if any.</param>
public class ReducerFailed(Type readModelType, IEnumerable<string> errorMessages, string stackTrace)
    : Exception($"Reducer for read model type '{readModelType.FullName}' failed: {string.Join("; ", errorMessages)}{(string.IsNullOrEmpty(stackTrace) ? string.Empty : Environment.NewLine + stackTrace)}");
