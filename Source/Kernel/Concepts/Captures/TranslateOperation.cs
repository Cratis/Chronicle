// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents a translate mapping operation.
/// </summary>
/// <param name="TargetProperty">The target property.</param>
/// <param name="SourceProperty">The source property.</param>
/// <param name="Translations">The translation entries.</param>
public record TranslateOperation(
    string TargetProperty,
    string SourceProperty,
    IReadOnlyList<TranslateValue> Translations) : MapOperation;
