// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The exception that is thrown when peer declarations disagree on a supplied artifact facet.
/// </summary>
/// <param name="artifactType">The artifact's CLR type.</param>
/// <param name="facet">The conflicting facet.</param>
/// <param name="firstSource">The first declaration's source.</param>
/// <param name="firstValue">The first supplied value.</param>
/// <param name="secondSource">The second declaration's source.</param>
/// <param name="secondValue">The second supplied value.</param>
public class ConflictingArtifactDeclaration(Type artifactType, string facet, string firstSource, string firstValue, string secondSource, string secondValue)
    : Exception($"'{artifactType.FullName}' has conflicting '{facet}' declarations: {firstSource} supplied '{firstValue}', while {secondSource} supplied '{secondValue}'.");
