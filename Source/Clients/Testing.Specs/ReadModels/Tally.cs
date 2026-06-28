// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Reducer-backed read model used to verify that <see cref="ReadModelScenario{TReadModel}"/>
/// seeds the reducer with the provided initial state.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="Count">Running count, incremented once per <see cref="Tallied"/> event.</param>
public record Tally(Guid Id, int Count);
