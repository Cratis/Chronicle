// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A reducer that takes an <see cref="IBonusProvider"/> as a constructor dependency, used to verify that
/// ReadModelScenario resolves reducer dependencies from its Services collection.
/// </summary>
/// <param name="bonusProvider">The injected bonus provider.</param>
public class BonusTallyReducer(IBonusProvider bonusProvider) : IReducerFor<BonusTally>
{
    /// <summary>
    /// Gets the unique identifier of the reducer.
    /// </summary>
    public ReducerId Id => "b0f5c9a1-2e3d-4a6b-9c8e-7f1a2b3c4d5e";

    /// <summary>
    /// Adds one plus the injected bonus to the running count for each <see cref="Tallied"/> event.
    /// </summary>
    /// <param name="event">The <see cref="Tallied"/> event.</param>
    /// <param name="current">The current <see cref="BonusTally"/> state, or <see langword="null"/> if none.</param>
    /// <param name="context">The <see cref="EventContext"/>.</param>
    /// <returns>The next <see cref="BonusTally"/> state.</returns>
    public BonusTally Increment(Tallied @event, BonusTally? current, EventContext context)
    {
        var increment = 1 + bonusProvider.GetBonus();
        return current is null
            ? new BonusTally(Guid.Empty, increment)
            : current with { Count = current.Count + increment };
    }
}
