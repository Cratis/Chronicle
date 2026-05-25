// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents a field rename mapping operation.
/// </summary>
/// <param name="SourceProperty">The source property.</param>
/// <param name="TargetProperty">The target property.</param>
public record FieldRenameOperation(string SourceProperty, string TargetProperty) : MapOperation;
