// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Captures;

/// <summary>
/// Represents a single translation entry.
/// </summary>
/// <param name="From">The source value.</param>
/// <param name="To">The translated value.</param>
public record TranslateValue(string From, string To);
