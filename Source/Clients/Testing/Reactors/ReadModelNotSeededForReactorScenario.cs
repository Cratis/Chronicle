// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// The exception that is thrown when a read-model handler parameter resolves to null in a class-based
/// reactor scenario without an explicit declaration of absence.
/// </summary>
/// <param name="readModelType">The read model type that was not seeded.</param>
/// <param name="key">The key used to resolve the read model.</param>
/// <param name="seededKeys">The keys seeded for the same read model type.</param>
public class ReadModelNotSeededForReactorScenario(Type readModelType, ReadModelKey key, IReadOnlyCollection<ReadModelKey> seededKeys)
    : Exception(CreateMessage(readModelType, key, seededKeys))
{
    static string CreateMessage(Type readModelType, ReadModelKey key, IReadOnlyCollection<ReadModelKey> seededKeys)
    {
        var seeded = seededKeys.Count > 0
            ? $" Seeded keys for this type: {string.Join(", ", seededKeys.Select(seededKey => $"'{seededKey.Value}'"))}."
            : string.Empty;
        return $"Read model '{readModelType.FullName}' was not seeded for key '{key.Value}'. " +
            "Seed it with Given.ForEventSourceId(id).ReadModel(...), using the resolved read-model key as id. " +
            "ReactorScenario does not build read models from given events, including passive read models. " +
            $"If absence is intentional, declare Given.ForEventSourceId(id).NoReadModel<{readModelType.Name}>().{seeded}";
    }
}
