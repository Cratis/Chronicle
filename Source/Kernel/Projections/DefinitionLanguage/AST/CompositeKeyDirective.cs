// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.LanguageDefinition.AST;

/// <summary>
/// Represents a composite key declaration.
/// </summary>
/// <param name="TypeName">The composite key type name.</param>
/// <param name="Parts">Collection of key parts.</param>
public record CompositeKeyDirective(TypeRef TypeName, IReadOnlyList<KeyPart> Parts) : ProjectionDirective;
