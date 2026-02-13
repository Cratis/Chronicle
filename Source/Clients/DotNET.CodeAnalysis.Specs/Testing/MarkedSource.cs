// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Text;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Testing;

/// <summary>
/// Represents parsed source and marker spans.
/// </summary>
/// <param name="Source">The cleaned source without markers.</param>
/// <param name="Markers">The marker span map.</param>
public record MarkedSource(string Source, IReadOnlyDictionary<int, TextSpan> Markers);
