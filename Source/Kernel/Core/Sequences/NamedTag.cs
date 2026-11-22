// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents a named tag on an append command.
/// </summary>
/// <param name="Name">The name of the tag.</param>
/// <param name="Value">The exact value of the tag.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Sequences.NamedTag")]
public record NamedTag([Id(0)] string Name, [Id(1)] string Value);
