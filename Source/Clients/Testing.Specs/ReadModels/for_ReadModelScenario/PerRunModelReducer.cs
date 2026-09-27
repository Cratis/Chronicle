// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// A reducer competing with an inline projection for the same read model.
/// </summary>
public class PerRunModelReducer : IReducerFor<PerRunModel>
{
    /// <summary>
    /// Gets the reducer identifier.
    /// </summary>
    public ReducerId Id => "d4637661-d50d-4879-876e-681fd67fb04e";

    /// <summary>
    /// Reduces a per-run event into the second property.
    /// </summary>
    /// <param name="event">The event to reduce.</param>
    /// <param name="current">The current state.</param>
    /// <returns>The reduced model.</returns>
    public PerRunModel Reduce(PerRunEvent @event, PerRunModel? current) => new(current?.First, @event.Value);
}
