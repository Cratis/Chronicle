// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Decision helpers for applying definition evolution plans.
/// </summary>
internal static class DefinitionEvolutionPolicyExtensions
{
    /// <summary>
    /// Determines whether a policy allows Chronicle to apply an operation automatically.
    /// </summary>
    /// <param name="policy">The configured policy.</param>
    /// <param name="operation">The planned operation.</param>
    /// <returns>True when Chronicle should apply the operation automatically.</returns>
    public static bool Allows(this DefinitionEvolutionPolicy policy, DefinitionEvolutionOperation operation) =>
        operation == DefinitionEvolutionOperation.NoAction ||
        policy == DefinitionEvolutionPolicy.Automatic ||
        (policy == DefinitionEvolutionPolicy.PartialOnly && operation == DefinitionEvolutionOperation.PartialReplay);
}
