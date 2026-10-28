// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Seeds a pre-built read model using canonical mapped composite key part names.
/// </summary>
/// <typeparam name="TReadModel">The read model type.</typeparam>
/// <param name="scenario">The scenario to seed.</param>
/// <param name="keyParts">The canonical mapped key parts.</param>
public class ReadModelKeyGivenBuilder<TReadModel>(ReadModelScenario<TReadModel> scenario, IReadOnlyDictionary<string, object?> keyParts)
    where TReadModel : class
{
    readonly IReadOnlyDictionary<string, object?> _keyParts = new Dictionary<string, object?>(keyParts, StringComparer.Ordinal);

    /// <summary>
    /// Registers a pre-built instance as the starting state of this composite key.
    /// </summary>
    /// <param name="readModel">The starting instance.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="InvalidReadModelSeedKey">The parts do not match the selected projection.</exception>
    /// <exception cref="ReadModelAlreadySeeded">The canonical key is already seeded.</exception>
    public Task ReadModel(TReadModel readModel)
    {
        scenario.CollectReadModelFor(_keyParts, readModel);
        return Task.CompletedTask;
    }
}
