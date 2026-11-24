// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the payload for causation.
/// </summary>
/// <param name="Occurred">The time and date for when it occurred.</param>
/// <param name="Type">The type of cause.</param>
/// <param name="Properties">Properties associated with the causation.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Sequences.Causation")]
public record Causation(
    [Id(0)] DateTimeOffset Occurred,
    [Id(1)] string Type,
    [Id(2)] IDictionary<string, string> Properties);
