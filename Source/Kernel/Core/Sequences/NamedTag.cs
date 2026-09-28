// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents a named tag on an append command.
/// </summary>
/// <param name="Name">The name of the tag.</param>
/// <param name="Value">The exact value of the tag.</param>
public record NamedTag(string Name, string Value);
