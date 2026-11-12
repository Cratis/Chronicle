// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Counts and sums contributions through the active reducer pipeline.
/// </summary>
[Reducer(Identifier)]
public sealed class ChaosReducer : IReducerFor<ChaosReductionModel>
{
    /// <summary>
    /// The stable observer identity shared with the drain oracle.
    /// </summary>
    public const string Identifier = "chronicle-chaos-reducer";

    /// <summary>
    /// Applies one contribution.
    /// </summary>
    /// <param name="event">The contribution.</param>
    /// <param name="current">Previous materialized state.</param>
    /// <returns>The updated state.</returns>
    public ChaosReductionModel Reduce(ChaosEvent @event, ChaosReductionModel? current) => new((current?.Count ?? 0) + 1, (current?.Sum ?? 0) + @event.Payload);
}
