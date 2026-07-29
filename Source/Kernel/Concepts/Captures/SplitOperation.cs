// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents a split mapping operation.
/// </summary>
/// <param name="SourceProperty">The source property.</param>
/// <param name="Separator">The separator to split by.</param>
/// <param name="TargetProperties">The target properties.</param>
public record SplitOperation(
    string SourceProperty,
    string Separator,
    IReadOnlyList<string> TargetProperties) : MapOperation;
