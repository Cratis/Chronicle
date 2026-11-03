// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Extension methods for validating <see cref="ReadModelContainerName"/>.
/// </summary>
public static class ReadModelContainerNames
{
    const string ForbiddenCharacters = "\"'`[];";

    /// <summary>
    /// Ensure a container name is usable as a table or collection name.
    /// </summary>
    /// <param name="name">The <see cref="ReadModelContainerName"/> to validate.</param>
    /// <param name="readModel">The identifier of the read model the name belongs to.</param>
    /// <exception cref="InvalidReadModelContainerName">Thrown when the name is empty, whitespace, or contains control, quote or delimiter characters.</exception>
    public static void EnsureValid(this ReadModelContainerName name, ReadModelIdentifier readModel)
    {
        if (string.IsNullOrWhiteSpace(name.Value) || name.Value.Any(IsForbidden))
        {
            throw new InvalidReadModelContainerName(readModel);
        }
    }

    static bool IsForbidden(char character) => char.IsControl(character) || ForbiddenCharacters.Contains(character);
}
