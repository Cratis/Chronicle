// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.CodeGeneration;

/// <summary>
/// Represents one property of a read model and how an event contributes to it.
/// </summary>
/// <param name="Property">The property path on the read model.</param>
/// <param name="Operation">What the event does to the property.</param>
/// <param name="Source">Where the value comes from, for the operations that take one.</param>
public record ProjectionPropertyMapping(
    string Property,
    ProjectionOperation Operation,
    ProjectionValueSource? Source);
