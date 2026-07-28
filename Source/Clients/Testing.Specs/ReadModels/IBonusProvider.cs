// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A service that <see cref="BonusTallyReducer"/> takes as a constructor dependency, used to verify that
/// ReadModelScenario resolves reducer dependencies registered in its Services collection.
/// </summary>
public interface IBonusProvider
{
    /// <summary>
    /// Gets the bonus added per tallied event.
    /// </summary>
    /// <returns>The bonus amount.</returns>
    int GetBonus();
}
