// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

/// <summary>
/// Reducer that increments <see cref="SeededJsonTally.Count"/> and carries the seeded payload forward untouched.
/// </summary>
public class SeededJsonTallyReducer : IReducerFor<SeededJsonTally>
{
    /// <summary>
    /// Gets the unique identifier of the reducer.
    /// </summary>
    public ReducerId Id => "6d1e8a4b-3c7f-4f2a-9b5d-0e8c1a7f2b64";

    /// <summary>
    /// Increments the running count.
    /// </summary>
    /// <param name="event">The <see cref="SeededJsonTallied"/> event.</param>
    /// <param name="current">The current <see cref="SeededJsonTally"/> state, or <see langword="null"/> if none.</param>
    /// <param name="context">The <see cref="EventContext"/>.</param>
    /// <returns>The next <see cref="SeededJsonTally"/> state.</returns>
    public SeededJsonTally Increment(SeededJsonTallied @event, SeededJsonTally? current, EventContext context) =>
        current is null ? new SeededJsonTally(Guid.Empty, 1, []) : current with { Count = current.Count + 1 };
}
