// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents a named tag criterion: the name is required, and any value must be requested explicitly.
/// </summary>
/// <param name="Name">The exact tag name.</param>
/// <param name="Values">Exact values, or null to match any value when <paramref name="AnyValue"/> is true.</param>
/// <param name="AnyValue">Whether the wire criterion explicitly matches any value for the name.</param>
public record NamedTagQueryCriterion(string Name, IEnumerable<string>? Values = null, bool AnyValue = false);
