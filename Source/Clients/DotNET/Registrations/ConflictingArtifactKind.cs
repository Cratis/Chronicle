// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The exception that is thrown when an explicit observer kind contradicts another declaration.
/// </summary>
/// <param name="artifactType">The artifact's CLR type.</param>
/// <param name="firstKind">The first declared kind.</param>
/// <param name="secondKind">The incompatible kind.</param>
public class ConflictingArtifactKind(Type artifactType, string firstKind, string secondKind)
    : Exception($"'{artifactType.FullName}' is declared as both '{firstKind}' and '{secondKind}'.");
