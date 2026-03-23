// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Migrations;

/// <summary>
/// Exception thrown when the upgrade type's generation is not exactly one more than the previous type's generation.
/// </summary>
/// <param name="previousType">The previous (older generation) CLR type.</param>
/// <param name="upgradeType">The upgrade (newer generation) CLR type.</param>
/// <param name="previousGeneration">The generation of the previous type.</param>
/// <param name="upgradeGeneration">The generation of the upgrade type.</param>
public class InvalidMigrationGenerationGap(
    Type previousType,
    Type upgradeType,
    EventTypeGeneration previousGeneration,
    EventTypeGeneration upgradeGeneration)
    : Exception(
        $"Migration from '{previousType.Name}' (generation {previousGeneration}) to '{upgradeType.Name}' (generation {upgradeGeneration}) is invalid. The upgrade generation must be exactly one more than the previous generation.");
